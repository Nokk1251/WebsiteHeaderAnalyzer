# Website Security Header Analyzer

[![CI](https://github.com/Nokk1251/WebsiteHeaderAnalyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/Nokk1251/WebsiteHeaderAnalyzer/actions/workflows/ci.yml)

Образователен C#/.NET конзолен инструмент, който извършва **пасивна** проверка на уебсайт: използва ли HTTPS и кои избрани HTTP security хедъри присъстват в отговора.

## Цел и целева аудитория

Инструментът е предназначен за начинаещи уеб разработчици и студенти по киберсигурност, които искат бърза и разбираема предварителна проверка. Повече подробности има в [docs/project-charter.md](docs/project-charter.md).

## Важно ограничение

Инструментът е **информационен** и работи само пасивно (изпраща обикновени HTTP заявки). Той не е penetration-testing инструмент, не е скенер за уязвимости и не е професионален одит. Наличието на security хедър **не доказва**, че даден сайт е сигурен, а липсата му не означава автоматично уязвимост.

Допълнителни ограничения на пасивния анализ:

- Анализира се само един отговор (финалният, след пренасочванията); не се обхождат други страници.
- Отчита се наличието и стойността на хедъра, но стойността **не се оценява** (например дали една Content-Security-Policy е достатъчно строга).
- Отговорът може да зависи от заглавията на заявката, местоположението или потребителя; различна заявка може да получи различни хедъри.

## Технологии

- C#, .NET 10, конзолно приложение
- `HttpClient` / `HttpClientHandler`
- xUnit за unit тестове

## Изграждане, стартиране и тестове

Изпълнявайте командите от корена на хранилището.

```powershell
dotnet restore .\Analyzer\Analyzer.slnx
dotnet build .\Analyzer\Analyzer.slnx --no-restore
dotnet test .\Analyzer\Analyzer.slnx
dotnet run --project .\Analyzer\Analyzer\Analyzer.csproj
```

При стартиране приложението поиска URL (напр. `https://example.com`), валидира го, изпраща пасивна `GET` заявка и показва отчет.

Код на изход: `0` при завършен анализ; `1` при невалиден вход, неуспешна заявка (мрежова грешка, таймаут, твърде много пренасочвания и др.) или неочаквана грешка. HTTP отговор с код като `404` е получен отговор и се отчита като завършен анализ.

## Проверявани хедъри

| Хедър |
|---|
| `Strict-Transport-Security` |
| `Content-Security-Policy` |
| `X-Content-Type-Options` |
| `X-Frame-Options` |
| `Referrer-Policy` |
| `Permissions-Policy` |

Търсенето на хедърите е без значение на главни/малки букви; хедърите от отговора и от съдържанието (content headers) се обединяват, а многократните стойности се запазват. За всеки хедър в кода има кратко описание на предназначението и ограничението му (`SecurityHeaderAnalyzer.Definitions`); към момента те **не се показват** в конзолния отчет.

## Примерен отчет

Реален изход при въвеждане на `http://github.com` (стойността на `Content-Security-Policy` е съкратена тук с `…`; приложението я показва изцяло, а стойностите на хедърите на даден сайт могат да се променят):

```text
Enter a URL:
=== Website analysis report ===
Initial URL: http://github.com/
Final URL: https://github.com/
Status code: 200 OK
Redirects followed: 1
HTTPS (final URL): Yes

Security headers (observations):
  [present] Strict-Transport-Security: max-age=31536000; includeSubdomains; preload
  [present] Content-Security-Policy: default-src 'none'; base-uri 'self'; …
  [present] X-Content-Type-Options: nosniff
  [present] X-Frame-Options: deny
  [present] Referrer-Policy: origin-when-cross-origin, strict-origin-when-cross-origin
  [missing] Permissions-Policy

Note: this is a passive check of a single response. It is not a security audit, and the presence of a header does not prove that a website is secure.
```

При неуспешна заявка отчетът съдържа началния адрес и съобщение за грешката, без секция с хедъри.

## Структура

```text
Analyzer/
├── Analyzer.slnx
├── Analyzer/                       # конзолното приложение
│   ├── Program.cs                  # вход: валидация -> HTTP анализ -> анализ на хедъри -> отчет
│   ├── UrlValidator.cs             # валидира и нормализира (trim) въведения URL
│   ├── ValidationResult.cs         # резултат от валидацията
│   ├── HttpAnalyzer.cs             # GET заявка, ръчни пренасочвания, събиране на хедъри, грешки
│   ├── HttpAnalysisResult.cs       # модел на резултата от HTTP анализа
│   ├── SecurityHeaderAnalyzer.cs   # наблюдения за избраните хедъри и техните описания
│   ├── SecurityHeaderDefinition.cs # име, предназначение и ограничение на хедър
│   ├── HeaderObservation.cs        # наблюдение: присъства/липсва и стойности
│   └── ConsoleReport.cs            # форматира текста на отчета
└── Analyzer.Tests/                 # xUnit тестове
docs/
└── project-charter.md              # цел, MVP и дефиниция за готовност
```

## Статус на функционалностите

| Функционалност | Статус |
|---|---|
| Валидация на URL (само абсолютни `http`/`https`, trim на интервали) | Реализирана |
| Модел на резултата от HTTP анализа (`HttpAnalysisResult`) | Реализиран |
| GET заявка с изключено автоматично пренасочване и таймаут 5 секунди | Реализирана |
| Ръчно проследяване на пренасочвания (лимит 10) и откриване на крайния URL | Реализирано |
| Изрична обработка на мрежови грешки, таймаут и прекъсване | Реализирана |
| Проверка дали крайният адрес използва HTTPS (включително понижаване от HTTPS към HTTP) | Реализирана |
| Анализ на избрани security хедъри | Реализиран |
| Описания и ограничения на хедърите в кода | Реализирани (не се показват в отчета) |
| Четим конзолен отчет и свързване в `Program.cs` | Реализирани |
| CI (build и тестове при Pull Request и push към `main`) | Реализиран |

## Как работи валидацията

`UrlValidator.Validate(string?)` връща `ValidationResult` (`IsValid`, `TrimmedUrl`, `Message`):

- празен вход или само интервали → невалиден ("The URL cannot be empty.");
- неабсолютен или некоректен URL → невалиден;
- схема, различна от `http`/`https` → невалиден;
- иначе → валиден, като `TrimmedUrl` съдържа входа без водещи и крайни интервали.

## Принос

Всяка промяна се разработва в отделен клон по съответния GitHub Issue и влиза в `main` само чрез Pull Request. Подробните правила са в [CLAUDE.md](CLAUDE.md).
