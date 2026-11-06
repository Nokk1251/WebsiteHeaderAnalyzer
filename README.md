# Website Security Header Analyzer

[![CI](https://github.com/Nokk1251/WebsiteHeaderAnalyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/Nokk1251/WebsiteHeaderAnalyzer/actions/workflows/ci.yml)

Образователен C#/.NET конзолен инструмент, който извършва **пасивна** проверка на уебсайт: използва ли HTTPS и кои избрани HTTP security хедъри присъстват в отговора.

> **Статус: проектът е в процес на разработка.** Реализирана е валидацията на URL. HTTP анализът е започнат, но още не е завършен и не е свързан с конзолното приложение (вижте [Статус на функционалностите](#статус-на-функционалностите)).

## Цел и целева аудитория

Инструментът е предназначен за начинаещи уеб разработчици и студенти по киберсигурност, които искат бърза и разбираема предварителна проверка. Повече подробности има в [docs/project-charter.md](docs/project-charter.md).

## Важно ограничение

Инструментът е **информационен** и работи само пасивно (изпраща обикновени HTTP заявки). Той не е penetration-testing инструмент, не е скенер за уязвимости и не е професионален одит. Наличието на security хедър **не доказва**, че даден сайт е сигурен, а липсата му не означава автоматично уязвимост.

## Технологии

- C#, .NET 10, конзолно приложение
- `HttpClient` / `HttpClientHandler`
- xUnit за unit тестове

## Структура

```text
Analyzer/
├── Analyzer.slnx
├── Analyzer/                 # конзолното приложение
│   ├── Program.cs            # вход: чете URL и показва резултата от валидацията
│   ├── UrlValidator.cs       # валидира и нормализира (trim) въведения URL
│   ├── ValidationResult.cs   # резултат от валидацията
│   ├── HttpAnalyzer.cs       # HTTP заявки (в процес на разработка)
│   └── HttpAnalysisResult.cs # модел на резултата от HTTP анализа
└── Analyzer.Tests/           # xUnit тестове
docs/
└── project-charter.md        # цел, MVP и дефиниция за готовност
```

## Статус на функционалностите

| Функционалност | Статус |
|---|---|
| Валидация на URL (само абсолютни `http`/`https`, trim на интервали) | Реализирана |
| Модел на резултата от HTTP анализа (`HttpAnalysisResult`) | Реализиран |
| HTTP заявка с изключено автоматично пренасочване и таймаут 5 секунди | Частично (`HttpAnalyzer`) |
| Ръчно проследяване на пренасочвания (лимит 10) и откриване на крайния URL | Не е завършено |
| Проверка дали крайният адрес използва HTTPS | Планирано |
| Анализ на избрани security хедъри | Планирано |
| Четим конзолен отчет | Планирано |

Към момента `Program.cs` само валидира въведения URL; `HttpAnalyzer` още не се извиква от него.

## Как работи валидацията

`UrlValidator.Validate(string?)` връща `ValidationResult` (`IsValid`, `TrimmedUrl`, `Message`):

- празен вход или само интервали → невалиден ("The URL cannot be empty.");
- неабсолютен или некоректен URL → невалиден;
- схема, различна от `http`/`https` → невалиден;
- иначе → валиден, като `TrimmedUrl` съдържа входа без водещи и крайни интервали.

## Изграждане, стартиране и тестове

Изпълнявайте командите от корена на хранилището.

```powershell
dotnet restore .\Analyzer\Analyzer.slnx
dotnet build .\Analyzer\Analyzer.slnx --no-restore
dotnet test .\Analyzer\Analyzer.slnx
dotnet run --project .\Analyzer\Analyzer\Analyzer.csproj
```

При стартиране приложението поиска URL (напр. `https://example.com`) и съобщава дали е валиден.

## Принос

Всяка промяна се разработва в отделен клон по съответния GitHub Issue и влиза в `main` само чрез Pull Request. Подробните правила са в [CLAUDE.md](CLAUDE.md).
