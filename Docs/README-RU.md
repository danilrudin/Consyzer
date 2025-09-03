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

## Принцип работы

1. Consyzer отбирает для анализа файлы, опираясь на заданную директорию и шаблоны поиска;
2. Consyzer логгирует и исключает из анализа файлы, не являющиеся сборками ECMA-355;
3. Consyzer анализирует оставшиеся ECMA-сборки на наличие P/Invoke-методов;
4. Consyzer анализирует каждый найденный P/Invoke-метод и проверяет наличие соответствующих нативных библиотек в системе;
5. Consyzer формирует отчёт по результатам анализа в одном или нескольких форматах в зависимости от конфигурации;
6. Consyzer возвращает код выхода, указывающий на конкретный результат анализа, что также позволяет осуществлять индивидуальную обработку инцидентов анализа в соответствии с Вашими требованиями.

> ⚠️ Анализ основан на метаданных CIL-сборок и не проверяет корректность маршалинга между управляемым и нативным кодом.

## Результаты анализа

**Consyzer** представляет результаты анализа в виде отчётов.  
Поддерживаются следующие форматы отчетов:

1. `Console`
2. `Json`
3. `Csv`
4. `Xml`

### Пример отчёта (Console)

```
[AssemblyMetadataList]
    [0]
        File: Foo.dll
        Version: 1.0.0.0
        CreationDateUtc: 21.06.2025 12:00:00
        Sha256: ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890
    [1]
        File: Bar.dll
        Version: 2.1.3.0
        CreationDateUtc: 22.06.2025 15:30:00
        Sha256: 1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF
    [2]
        File: Baz.dll
        Version: 1.2.0.0
        CreationDateUtc: 23.06.2025 10:45:00
        Sha256: FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321
[PInvokeMethodGroups]
    [0] File: Foo.dll, Found: 2
        [0]
            Signature: 'Int32 static Native.Foo.DoStuff()'
            ImportName: 'existentlib.dll'
            ImportFlags: 'CallingConventionCDecl'
        [1]
            Signature: 'Void static Native.Foo.FailStuff(String)'
            ImportName: 'missinglib.dll'
            ImportFlags: 'CallingConventionStdCall'
    [1] File: Baz.dll, Found: 1
        [0]
            Signature: 'Boolean static .Baz.CheckSomething(Int32)'
            ImportName: 'anotherlib.dll'
            ImportFlags: 'CallingConventionStdCall'
[LibraryPresences]
    [0]
        LibraryName: existentlib.dll
        ResolvedPath: C:\Windows\System32\existentlib.dll
        LocationKind: InSystemDirectory
    [1]
        LibraryName: missinglib.dll
        ResolvedPath: null
        LocationKind: Missing
    [2]
        LibraryName: anotherlib.dll
        ResolvedPath: C:\EnvPath\anotherlib.dll
        LocationKind: InEnvironmentPath
[Summary]
    TotalFiles: 3
    EcmaAssemblies: 3
    AssembliesWithPInvoke: 2
    TotalPInvokeMethods: 3
    MissingLibraries: 1
```

## Коды возврата

**Consyzer** возвращает конкретный код выхода в зависимости от того, где были или не были обнаружены нативные библиотеки, указанные в атрибутах P/Invoke:

| Код | Значение анализа                                                            |
| --- | --------------------------------------------------------------------------- |
| 0   | Все библиотеки обнаружены в анализируемой директории                        |
| 1   | Одна или несколько библиотек обнаружены в системной директории              |
| 2   | Одна или несколько библиотек обнаружены через переменную окружения **PATH** |
| 3   | Одна или несколько библиотек обнаружены по абсолютному пути                 |
| 4   | Одна или несколько библиотек обнаружены по относительному пути              |
| 5   | Одна или несколько библиотек отсутствуют в системе                          |

> Коды анализа соответствуют порядку, в котором **Consyzer** ищет библиотеки в системе.  
> Если библиотеки найдены в разных местах, возвращается наибольший из соответствующих кодов.  
> Только последний из кодов означает, что хотя бы одна библиотека не была найдена.

---

| Код | Ошибка конфигурации или выполнения                         |
| --- | ---------------------------------------------------------- |
| -1  | Не указана директория для анализа                          |
| -2  | Не указаны шаблоны поиска файлов                           |
| -3  | Не найдено файлов в директории по заданным шаблонам поиска |
| -4  | Не найдено допустимых файлов для анализа                   |
| -5  | Не найдено P/Invoke методов в анализируемых сборках        |

> Отрицательные коды сигнализируют об ошибках конфигурации или сбоях, возникших в процессе работы утилиты.  
> Чем меньше значение кода, тем **дальше зашла утилита до возникновения ошибки**.

### Использование

**Consyzer** запускается из командной строки (CLI) и требует два обязательных параметра:

1. `--AnalysisDirectory` — задает директорию, содержащую CIL-модули для анализа;
2. `--SearchPatterns` — задает шаблоны поиска CIL-модулей для анализа.

Вы также можете указать два дополнительных параметра:

1. `--RecursiveSearch` — указывает, выполнять ли поиск CIL-модулей во вложенных директориях. По умолчанию: `false`.
2. `--ReportFormats` — задает форматы вывода отчёта (`Console`, `Json`, `Csv`, `Xml`) в виде списка, разделенного запятыми. По умолчанию: `Console`.

### Общий шаблон запуска

```
Consyzer.exe --AnalysisDirectory <путь_к_директории> --SearchPatterns <шаблоны_поиска> [--RecursiveSearch true|false] [--ReportFormats Console, Json, Csv, Xml]
```

### Пример

```
Consyzer.exe --AnalysisDirectory C:\Modules --SearchPatterns "*.dll, *.exe" --RecursiveSearch true --ReportFormats Console, Json
```

## Анализ нескольких проектов в решении

Вы можете использовать [этот](./DevOps/Scripts/SolutionAnalyzer.ps1) сценарий PowerShell для анализа выходных артефактов всех проектов в решении.  
Этот сценарий может быть также использован в **конвейере CI/CD**.
