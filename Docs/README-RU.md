[![Build Status](https://github.com/danilrudin/Consyzer/workflows/Build/badge.svg)](https://github.com/danilrudin/Consyzer/actions/workflows/build.yml) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=coverage)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![GitHub license](https://img.shields.io/github/license/danilrudin/Consyzer)](https://github.com/danilrudin/Consyzer/blob/master/LICENSE)

## Обзор

**Consyzer** — это CLI-утилита, созданная для предотвращения проблем консистентности CIL-модулей при использовании механизмов P/Invoke для вызова методов, реализованных вне управляемой среды CLR.

## Назначение

При разработке CIL-приложений нередко возникают ситуации, требующие обращения к методам, реализованным вне управляемой экосистемы .NET. В исходном коде CIL-модуля такие вызовы описываются атрибутами **DllImport** или **LibraryImport** и сохраняются в метаданных модуля после его сборки, указывая, к какой именно неуправляемой (нативной) библиотеке следует обратиться во время выполнения и какая функция из нее должна быть вызвана.

Ключевой особенностью подобных вызовов является то,
что код функции, вызываемой из неуправляемой библиотеки, не компонуется с исходным кодом CIL-модуля напрямую;
вместо этого в метаданных модуля сохраняется информация о вызываемой функции, включая ссылку на ожидаемое местоположение неуправляемой библиотеки, содержащей реализацию этой функции, в системе.

```csharp
// В данном примере "foo.dll" является ссылкой на неуправляемую библиотеку, содержащую реализацию функции HelloWorld:

// Классический P/Invoke
[DllImport("foo.dll")]
public static extern void HelloWorld();

// или

// Source-generated P/Invoke (.NET 7+)
[LibraryImport("foo.dll")]
public static partial void HelloWorld();
```

Приложение функционирует корректно, не нарушая целостность и безопасность системы, когда все неуправляемые библиотеки находятся на местах, описанных в метаданных;
однако, если хотя бы одна из библиотек отсутствует, приложение не только завершит свою работу аварийно, но и может привести к нарушению безопасности всей системы.

Consyzer был разработан для того, чтобы подобные ситуации не стали неожиданностью.

### Поддерживаемые платформы

На данный момент Consyzer поддерживает проверку наличия нативных библиотек в системе на следующих платформах:

- Windows
- Linux

## Принцип работы

1. Consyzer отбирает для анализа файлы, опираясь на заданную директорию и шаблоны поиска;
2. Consyzer логгирует и исключает из анализа файлы, не являющиеся сборками ECMA-355;
3. Consyzer анализирует оставшиеся ECMA-сборки на наличие P/Invoke-методов;
4. Consyzer анализирует каждый найденный P/Invoke-метод и проверяет наличие соответствующих нативных библиотек в системе;
5. Consyzer формирует отчёт по результатам анализа в одном или нескольких форматах в зависимости от конфигурации;
6. Consyzer возвращает код выхода, указывающий на итоговый результат анализа, что также позволяет осуществлять индивидуальную обработку инцидентов анализа в соответствии с Вашими требованиями.

> ⚠️ Анализ основан на метаданных CIL-сборок и не проверяет корректность маршалинга между управляемым и нативным кодом.

## Модель поиска библиотек

**Consyzer** использует строгую модель анализа библиотек: найденной считается только та библиотека, местоположение которой удалось явно определить через поддерживаемые механизмы поиска.

Результат проверки наличия каждой нативной библиотеки может иметь одно из следующих состояний:

| Состояние      | Значение анализа                                                                                                              |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `Resolved`     | Библиотека обнаружена через поддерживаемый механизм поиска                                                                    |
| `Missing`      | Библиотека не обнаружена, и Consyzer не знает неподдерживаемых механизмов, которые могли бы изменить результат                |
| `Inconclusive` | Библиотека не обнаружена, но результат нельзя считать окончательным, т.к. Consyzer не может симулировать часть механизмов ОС. |

Consyzer так же указывает механизм, через который было обнаружено наличие библиотеки в системе:

| Механизм                 | Значение анализа                                             |
| ------------------------ | ------------------------------------------------------------ |
| `ExplicitPath`           | Библиотека обнаружена на пути, указанном при её импорте      |
| `AssemblyDirectory`      | Библиотека обнаружена рядом со сборкой, объявляющей P/Invoke |
| `DefaultSystemLocations` | Библиотека обнаружена в стандартных директориях ОС           |
| `EnvironmentOverride`    | Библиотека обнаружена через переменную окружения             |
| `CurrentDirectory`       | Библиотека обнаружена в текущей рабочей директории (Windows) |

Некоторые из существующих механизмов загрузки ОС или не моделируются полностью и будут добавлены в будущих версиях, или не будут добавлены вовсе из-за недоступности воспроизведения статическим анализом.
Например, на Windows это могут быть `KnownDLLs`, `SxS`, перенаправления DLL и настройки директории поиска процесса,
а на Linux — `RPATH`, `RUNPATH`, `ld.so.cache`, `ld.so.conf` и прочие особенности защищённого выполнения.

Если библиотека не была найдена, но существуют несимулируемые механизмы, способные повлиять на результат,
Consyzer зарегистрирует такую библиотеку как `Inconclusive`, а не выдаст предположение за гарантированный результат.

Также Consyzer может показать эвристические совпадения — например, библиотеку в корне анализа, которая не находится рядом с вложенной target-сборкой.
Такие совпадения также регистрируются отдельно и не превращают результат строгой проверки в успешный.

## Результаты анализа

**Consyzer** представляет результаты анализа в виде отчётов.
Поддерживаются следующие форматы отчетов:

1. `Console`
2. `Json`
3. `Csv`
4. `Xml`

### Пример отчёта (Console)

```
[Analysis]
    Platform: Windows
[AssemblyMetadataList]
    [0]
        File: Foo.dll
        Version: 1.0.0.0
        CreationDateUtc: 2025-06-21T12:00:00.0000000Z
        Sha256: ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890
    [1]
        File: Bar.dll
        Version: 2.1.3.0
        CreationDateUtc: 2025-06-22T15:30:00.0000000Z
        Sha256: 1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF
    [2]
        File: Baz.dll
        Version: 1.2.0.0
        CreationDateUtc: 2025-06-23T10:45:00.0000000Z
        Sha256: FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321
[PInvokeMethodGroups]
    [0]
        File: Foo.dll, Found: 2
        [0]
            Signature: 'Int32 static Native.Foo.DОСtuff()'
            ImportName: 'existentlib.dll'
            ImportFlags: 'CallingConventionCDecl'
        [1]
            Signature: 'Void static Native.Foo.FailStuff(String)'
            ImportName: 'missinglib.dll'
            ImportFlags: 'CallingConventionStdCall'
    [1]
        File: Baz.dll, Found: 1
        [0]
            Signature: 'Boolean static .Baz.CheckSomething(Int32)'
            ImportName: 'anotherlib.dll'
            ImportFlags: 'CallingConventionStdCall'
[LibraryResolutions]
    [0]
        TargetPath: C:\Modules\Foo.dll
        LibraryName: existentlib.dll
        ResolutionState: Resolved
        ResolvedPath: C:\Windows\System32\existentlib.dll
        MechanismKind: DefaultSystemLocations
        HeuristicCandidates: []
        NotSimulated: None
    [1]
        TargetPath: C:\Modules\Foo.dll
        LibraryName: missinglib.dll
        ResolutionState: Inconclusive
        ResolvedPath: null
        MechanismKind: null
        HeuristicCandidates: []
        NotSimulated: WindowsSxS, WindowsKnownDlls, WindowsDllRedirection
    [2]
        TargetPath: C:\Modules\Baz.dll
        LibraryName: anotherlib.dll
        ResolutionState: Resolved
        ResolvedPath: C:\EnvPath\anotherlib.dll
        MechanismKind: EnvironmentOverride
        HeuristicCandidates: []
        NotSimulated: None
[Summary]
    TotalFiles: 3
    EcmaAssemblies: 3
    AssembliesWithPInvoke: 2
    TotalPInvokeMethods: 3
    ResolvedLibraries: 2
    MissingLibraries: 0
    InconclusiveLibraries: 1
```

## Коды возврата

**Consyzer** возвращает конкретный код выхода в зависимости от итогового состояния анализа:

| Код | Значение анализа                                                                                                                    |
| --- | ----------------------------------------------------------------------------------------------------------------------------------- |
| 0   | Все библиотеки обнаружены через поддерживаемые механизмы поиска                                                                     |
| 1   | Одна или несколько библиотек отсутствуют                                                                                            |
| 2   | Одна или несколько библиотек не найдены проверяемыми механизмами, но могут быть обнаружены не симулируемыми механизмами загрузки ОС |
| 3   | Ошибка входных параметров                                                                                                           |
| 4   | Ошибка выполнения утилиты                                                                                                           |

> Если среди результатов есть хотя бы один `Missing`, возвращается код `1`.
> Если `Missing` нет, но есть хотя бы один `Inconclusive`, возвращается код `2`.
> Код `0` возвращается только тогда, когда все найденные P/Invoke-зависимости имеют состояние `Resolved`.

### Использование

**Consyzer** запускается из командной строки (CLI) и требует два обязательных параметра:

1. `--AnalysisDirectory` — задает директорию, содержащую CIL-модули для анализа;
2. `--SearchPatterns` — задает шаблоны поиска CIL-модулей для анализа.

Вы также можете указать два дополнительных параметра:

1. `--RecursiveSearch` — указывает, выполнять ли поиск CIL-модулей во вложенных директориях. По умолчанию: `false`.
2. `--ReportFormats` — задает форматы вывода отчёта (`Console`, `Json`, `Csv`, `Xml`) в виде списка, разделенного запятыми. По умолчанию: `Console`.

### Общий шаблон запуска

Windows:

```powershell
Consyzer.exe --AnalysisDirectory <путь_к_директории> --SearchPatterns <шаблоны_поиска> [--RecursiveSearch true|false] [--ReportFormats Console,Json,Csv,Xml]
```

Linux:

```bash
./Consyzer --AnalysisDirectory <путь_к_директории> --SearchPatterns <шаблоны_поиска> [--RecursiveSearch true|false] [--ReportFormats Console,Json,Csv,Xml]
```

### Пример

```powershell
Consyzer.exe --AnalysisDirectory C:\Modules --SearchPatterns "*.dll,*.exe" --RecursiveSearch true --ReportFormats Console,Json
```

```bash
./Consyzer --AnalysisDirectory ./modules --SearchPatterns "*.dll,*.exe" --RecursiveSearch true --ReportFormats Console,Json
```

## Анализ нескольких проектов в решении

Вы можете использовать [этот](../DevOps/Scripts/SolutionAnalyzer.ps1) сценарий PowerShell для анализа выходных артефактов всех проектов в решении.
Этот сценарий может быть также использован в **конвейере CI/CD**.
