# Peer to peer uložiště

Cílem tohoto projektu je uložiště, který bude fungovat zcela bez centrálních serverů.
Ukládání souborů funguje tak že soubor se zkompresuje, rozdělí se na několik částí a ty se následně pošlou na ostatní zařízení, které jsou v této síti připojeny.
Když si chceme soubor stáhnout, stáhneme si všehchny koušíčky ze všech zařízeních, poskládáme je, a máme náš soubor.

---

### Ovládání je jednoduché pomocí příkazů(v programu je dostupný příkaz "help" pro výpis a vysvětlení všech příkazů):
* join - pokud nemáte žádně další nody z bootnode nebo si chce přidat další node, tak jí můžete přidat pomocí ip adresy a port node
* joinAll - vezme známé nody od uložených node a nové si uloží
* upload - zadá se bud absolutní cesta k souboru nebo název souboru, který je v adresáři programu, a ten se následně pošlě na ostatní nodes, které daný zařízení zná
* download - zadá se název manifest a podle něho se stáhnout části ze všech nodes a vytvoří z nich soubor a ten se uloží do adresáře programu do složky files
* delete - smaže části souborů ze všech node
* settings - možnost nastavení portu aplikace a maximálního čísla do kolika zařízení se může jeden soubor poslat(v konfiguračním souboru App.config jde nastavit ip adresy bootNode)
* exit - vypnutí aplikace
