# Navod2 – Editor Assistant pro palubní literaturu Škoda

## Přehled projektu

Desktopová aplikace pro redaktory palubní literatury Škoda (návody k obsluze vozidel). Pomáhá s kontrolou textu, správou zakázaných slov, kontrolou gramatiky a porovnáváním verzí dokumentů.

## Technologický stack

- **Platform:** Windows desktop
- **Framework:** WPF (.NET 8, C#)
- **UI pattern:** MVVM (CommunityToolkit.Mvvm)
- **Parsování XML:** `System.Xml.Linq` (LINQ to XML)
- **Parsování HTML:** `HtmlAgilityPack`
- **Parsování PDF:** `PdfPig`
- **Gramatika:** LanguageTool REST API – **lokální instance via standalone Java JAR**
- **Diff/porovnání:** `DiffPlex` (NuGet)
- **Konfigurace zakázaných slov:** JSON soubor, `System.Text.Json`
- **Projekt soubor:** `Navod2.sln` / `Navod2.csproj`

## Zdrojová data

Umístění: `source/`

### Formáty vstupních dokumentů

| Formát | Popis |
|--------|-------|
| `.xml` | VW DOCUFY K4 TREE – hlavní XML export z redakčního systému COSIMA |
| `.pdf` | PDF verze návodu (alternativní zdroj) |
| `.zip` | ZIP archiv s HTML soubory – jeden HTML soubor na téma/modul |

### Struktura XML (DOCUFY K4 TREE)

DOCTYPE: `-//DOCUFY VW//DTD K4 TREE//DE`

Klíčové XML elementy nesoucí text:
- `<p>` – odstavec textu
- `<titel>` – nadpis/titulek
- `<block>` – blok obsahu
- `<node name="...">` – kapitola/uzel stromu dokumentu
- `<topic-*>` – modul tématu (topic-umschlag, topic-regular, atd.)
- `<warnung>`, `<vorsicht>`, `<hinweis>` – bezpečnostní upozornění (ANSI)

Atributy identifikace: `y.id`, `y.io.id`, `y.io.language="cs"`, `y.io.variant="CZ"`

### Struktura ZIP (HTML)

- Každý soubor: `{hash}_{version}_cs_cz.html`
- Formát: TopicPilot HTML export
- Obsahuje i obrázky (inline nebo jako reference)

## Funkce aplikace

### 1. Načítání dokumentu

- Drag & drop nebo dialog pro výběr souboru
- Podpora formátů: `.xml`, `.pdf`, `.zip`
- Po načtení se zobrazí stromová struktura kapitol (pro XML/HTML)
- Textový obsah se extrahuje a uloží do interní reprezentace

**Extrakce textu z XML:**
- Projít celý strom XML
- Z každého `<p>`, `<titel>`, `<block>` extrahovat textový obsah (ignorovat atributy)
- Zachovat kontext: ID uzlu, název kapitoly
- Ignorovat prázdné elementy `<p>&#160;</p>`
- Filtrovat dle `y.io.language="cs"` a `y.io.variant="CZ"`

### 2. Správa zakázaných slov

- Uložena v: `data/forbidden-words.json`
- Struktura záznamu:
  ```json
  {
    "id": "uuid",
    "word": "string",
    "suggestion": "string (správná alternativa, volitelně)",
    "reason": "string (proč je slovo zakázané, volitelně)",
    "caseSensitive": false,
    "category": "string (terminologie | styl | zastaralé | ...)"
  }
  ```
- UI pro CRUD operace (přidat, editovat, smazat, vyhledat, filtrovat dle kategorie)
- Exportovat/importovat seznam jako CSV nebo JSON
- Předvyplněný výchozí seznam dle Redaktorské příručky (viz sekce Terminologie níže)

### 3. Kontrola zakázaných slov

- Prohledat načtený dokument pro každé zakázané slovo (celé slovo, case-insensitive dle nastavení)
- Výsledky zobrazit jako seznam nálezů:
  - Zakázané slovo + navrhovaná alternativa
  - Kontext (věta nebo odstavec)
  - Kapitola / ID uzlu
  - Možnost navigovat na místo v dokumentu
- Zvýraznit nálezy v zobrazeném textu
- Export výsledků do CSV nebo Excel

### 4. Kontrola české gramatiky

**LanguageTool – lokální instance (doporučeno):**
- Stáhnout LanguageTool Desktop/Server JAR z https://languagetool.org/download/
- Spustit: `java -cp languagetool-server.jar org.languagetool.server.HTTPServer --port 8081`
- Aplikace spustí LanguageTool automaticky při startu (pokud je nakonfigurovaná cesta k JAR)
- Fallback: cloud API `https://api.languagetool.org/v2/check` (rate limit: 20 req/min)

**Výhody lokální instance:**
- Žádné internetové připojení (firemní dokumenty neopouštějí síť)
- Žádné rate limity
- Nižší latence

**Integrace:**
- `POST http://localhost:8081/v2/check` s parametry `language=cs&text={obsah}`
- Zpracovat výsledky a zobrazit:
  - Chyba / varování / návrh
  - Kontext s chybou (offset + délka)
  - Navrhovaná oprava
  - Kategorie chyby (gramatika, styl, typografie)
- Kontrolovat po odstavcích, výsledky agregovat

### 5. Porovnání dvou verzí dokumentu

- Možnost načíst dvě verze (Dokument A vs. Dokument B)
- Porovnat na úrovni:
  - Shoda kapitol/nodů dle `y.id` z XML (primárně) nebo dle názvu
  - Diff textového obsahu uvnitř párovaných nodů
- Zobrazit rozdíly side-by-side nebo inline (added/removed/changed)
- Použít knihovnu `DiffPlex` pro diff algoritmus
- Zobrazit statistiku: počet přidaných/odebraných/změněných odstavců
- Export diff reportu

### 6. Kontrola formátu čísel a typografie

Pravidla dle Redaktorské příručky 1.53 (sekce 5 – Typografická pravidla):

#### Psaní čísel
| Pravidlo | Správně ✓ | Špatně ✗ |
|----------|-----------|----------|
| Desetinná čárka (ne tečka) | `123,5` | `123.5` |
| 4místná čísla nedělit | `9000` | `9 000` |
| 5+ místná čísla dělit po 3 (tvrdá mezera) | `100 000` | `100000` nebo `100.000` |
| Rozsah bez mezer, pomlčka ndash | `110–6000` | `110 – 6000` nebo `110-6000` |
| Záporné číslo: minus (ndash) bez mezery za | `-10 °C` | `– 10 °C` |
| Číslovky přednostně číslicemi | `3 týdny` | `tři týdny` (u čísel) |

#### Psaní jednotek
| Pravidlo | Správně ✓ | Špatně ✗ |
|----------|-----------|----------|
| Jednotka jako zkratka (je-li číslo) | `10 km`, `8 h`, `30 min`, `10 s` | `10 kilometrů` |
| Jednotka slovem (není-li číslo) | `zobrazení zbývajících km a dnů` | `zobrazení zbývajícího počtu km` |
| Zkratky bez tečky, odděleny tvrdou mezerou | `10 km`, `2,5 l` | `10km`, `10 km.` |
| Nadmořská výška vždy `m n. m.` s tvrdými mezerami | `1000 m n. m.` | `1000 m.n.m.` |
| Stupně (entita `deg`) | `5 °C` | `5°C` nebo `5 C` |
| Procenta: mezera před % | `15 %` | `15%` |

#### Interpunkce a speciální znaky
| Pravidlo | Správně ✓ | Špatně ✗ |
|----------|-----------|----------|
| Pomlčka v textu = ndash s mezerami | `sedadlo – zrcátka` | `sedadlo - zrcátka` |
| Rozsah hodnot = ndash bez mezer | `110–120 km/h` | `110 - 120 km/h` |
| Nepoužívat středník | `.` nebo nová věta | `;` |
| Lomítko u jednoslovných výrazů bez mezer | `Vyklopení/sklopení` | `Vyklopení / sklopení` |
| Lomítko u víceslovných výrazů s mezerami | `Ukončení hovoru / Odmítnutí hovoru` | `Ukončení hovoru/Odmítnutí hovoru` |
| Max. 17 slov ve větě | — | — |

#### Max./Min.
- `Max.` / `Min.` použít před číselným údajem nebo v tabulce
- `Maximální` / `Minimální` použít bez čísla v textu

## Terminologie – předvyplněný seznam zakázaných slov

Dle Redaktorské příručky 1.53 (sekce 10 – Terminologie):

### Zakázané výrazy → správné alternativy

| Zakázané slovo | Správná alternativa | Poznámka |
|----------------|---------------------|----------|
| Plynový pedál | Akcelerační pedál | |
| Zážehový motor | Benzinový motor | |
| Za jízdy | Během jízdy | |
| Čelní okno | Čelní sklo | |
| Monochromatický displej | Černobílý displej | |
| Vznětový motor | Dieselový motor | |
| Webové stránky | Internetové stránky | |
| Je možno, lze | Je možné, Můžete | výjimka: právní texty |
| V opačném případě | Jinak | |
| Karosérie | Karoserie | |
| Klíč na kola | Klíč na šrouby kol | |
| Kontrolka, Kontrolní symbol | Kontrolní světlo | |
| Ruční | Manuální | výjimka: ruční mytí |
| Mechanismus | Mechanizmus | |
| Diesel | Nafta | |
| Běžící motor | Nastartovaný motor | |
| Vyřazení z funkce | Nefunkční / Negativně ovlivněná funkce | |
| Nevíste | Nevíte | |
| Přístrojová deska | Palubní deska | |
| Přístrojový panel | Panel přístrojů | |
| 4x4, Pohon 4x4 | Pohon všech kol | výjimka: v tabulkách |
| Plat. pro vozy 4x4 | Platí pro vozy s pohonem všech kol | |
| Řízení vpravo/vlevo | Pravostr./levostr. řízení | |
| Přiřazení/Odebrání | Přidání/Odebrání | výjimka: přiřazení loga |
| Přijmutí (hovoru) | Přijetí (hovoru) | |
| Mód | Režim | |
| Senzor, čidlo | Snímač | |
| Totožný | Stejný | |
| Krátké/dlouhé stisknutí | Stisknutí/přidržení | |
| Střední konzole | Středová konzola | |
| Textýlie | Textilie | |
| Motorizace | Typ motoru | |
| Neutrál (hovorové) | Řadicí páka v neutrální poloze | |
| Z výrobního závodu | Z výroby | |
| Zvolený rychlostní stupeň | Zařazený rychlostní stupeň | |
| Opět, opětovně | Znovu | POZOR: „opětovně" a „opětovným" jsou v pořádku |
| Otevření/Zavření | Zobrazení/Zavření | pro displej |
| Zesílení | Zvýšení | pro intenzitu, hlučnost, riziko |
| Kromě toho | Také | |
| Provést + podst. jméno | Konkrétní sloveso | nap. „Deaktivujte ASR" místo „Proveďte deaktivaci ASR" |
| Resp. | Nebo / lomítko | výjimka: popis dvou variant (limuzína, resp. kombi) |
| Přibližně asi | Přibližně | |
| Bezpodmínečně | (vynechat) | nadbytečné slovo |
| Prosím | (vynechat) | nadbytečné slovo |

### Stylistická pravidla detekovaná automaticky
- Dvojitý zápor (není ... nezapnutá) → nahradit přímým vyjádřením
- Způsobová slovesa (musíte, musí) → pokud možno přímý pokyn
- Vazba „je třeba/je nutné + infinitiv" → přímý pokyn
- Podmiňovací způsob (by) → indikativ
- Závorky → přepsat do výčtu nebo věty

## Architektura projektu

```
Navod2/
├── CLAUDE.md
├── Navod2.sln
├── src/
│   ├── Navod2.App/          # WPF aplikace (Views, ViewModels)
│   ├── Navod2.Core/         # Business logika (parsery, kontroly)
│   └── Navod2.Tests/        # Unit testy
├── data/
│   └── forbidden-words.json # Perzistentní seznam zakázaných slov
├── tools/
│   └── languagetool/        # LanguageTool JAR (lokální instance, není v gitu)
├── source/                  # Zdrojové dokumenty (v .gitignore – jsou příliš velké)
└── redaktorská pravidla/    # Redaktorská příručka PDF
```

### Vrstvení (Navod2.Core)

```
Parsers/
  XmlDocumentParser.cs      # DOCUFY K4 TREE XML parser
  HtmlZipParser.cs          # ZIP s HTML soubory
  PdfParser.cs              # PDF parser

Models/
  DocumentNode.cs           # Uzel dokumentu (id, title, text, children)
  ForbiddenWord.cs          # Záznam zakázaného slova
  CheckResult.cs            # Výsledek kontroly (nález v textu)
  DiffResult.cs             # Výsledek porovnání verzí

Services/
  ForbiddenWordService.cs   # CRUD pro zakázaná slova (JSON perzistence)
  GrammarCheckService.cs    # LanguageTool integrace (lokální JAR + HTTP)
  DocumentDiffService.cs    # Porovnání dvou verzí dokumentu
  NumberFormatService.cs    # Kontrola formátu čísel a typografie
  LanguageToolHostService.cs # Spuštění/zastavení lokálního LanguageTool procesu
```

## Klíčové implementační poznámky

### XML parsování
- Soubor je velký (~6 MB), používat `XDocument.Load()` – pro tuto velikost dostačující
- HTML entity jsou standardní XML entity – XDocument je dekóduje automaticky
- Ignorovat prázdné elementy `<p>&#160;</p>` (obsahují jen nezlomitelnou mezeru)
- Atribut `y.io.language="cs"` a `y.io.variant="CZ"` použít k filtrování české varianty

### LanguageTool lokální instance
- Stáhnout `LanguageTool-*.zip` z https://languagetool.org/download/
- Spuštění: `java -jar languagetool-server.jar --port 8081 --allow-origin "*"`
- Aplikace spustí LanguageTool jako child proces (`Process.Start`) při startu a ukončí při zavření
- Cesta k JAR konfigurovatelná v nastavení aplikace
- Rate limiting zbytečný u lokální instance; pro cloud: max 20 req/min (free tier)
- Automobilová terminologie může generovat false positives – přidat whitelist výrazů

### Diff algoritmus
- Porovnávat na úrovni odstavců (paragraph-level diff) pomocí DiffPlex
- Pro mapování kapitol mezi verzemi: párovat primárně dle `y.io.id` z XML (stabilní ID z COSIMA)
- Fallback: párování dle názvu kapitoly (`name` atribut `<node>`)

### Kontrola čísel – detekční regex vzory
- Desetinná tečka v čísle: `\d+\.\d+` (upozornit, pokud není v URL nebo kódu)
- Chybějící mezera v 5+místném čísle: `\d{5,}` bez mezery uvnitř
- Jednota bez mezery od čísla: `\d+(km|m|cm|mm|kg|l|ml|°C|%|kW|kWh)` (bez mezery)
- Procento bez mezery: `\d+%`
- Rozsah se spojovníkem: `\d+-\d+` (místo ndash)

### Výkon
- Načítání velkého XML v background tasku (`Task.Run` + `IProgress<T>`)
- Progress bar během načítání a kontrol
- Výsledky zobrazovat průběžně (ObservableCollection aktualizovaná z UI threadu)

## Vývoj

```bash
# Sestavení
dotnet build

# Testy
dotnet test

# Spuštění aplikace
dotnet run --project src/Navod2.App
```

## Otevřené otázky

- [ ] Upřesnit UI layout – záložky (TabControl) nebo sidebar + content panel
- [ ] Podpora více jazyků UI? (pravděpodobně CZ only)
- [ ] Upřesnit, zda má aplikace ukládat historii kontrol (logy výsledků)
- Java je dostupná na strojích redaktorů ✓
