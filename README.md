# Empyrion Structure Clean Up
## FAQ

Eine übersetzte Version findet ihr im EmpyrionStructureCleanUp/bin Verzeichnis falls ihr die Mod nicht selbst prüfen und kompilieren wollt ;-)

Oder hier: https://empyriononline.com/threads/mod-empyrionstructurecleanup-in-shared-folder.45338/

### Wo für ist das?

Empyrion lässt alle Strukturen im Shared-Verzeichnis stehen auch wenn diese im Spiel nicht mehr referenziert sind. 
Somit wird dieses Verzeichnis immer größer und enthalten immer mehr Unterverzeichnisse und Dateien.
Dies hat Einfluss auf die Größe und die Dauer eines Backups und auch die Performance der Dateizugriffe leidet 
ab einer gewissen Anzahl. Ebenso geht der Überblick verloren da man den Dateien und Verzeichnissen nicht
"ansehen" kann ob sie noch verwendet werden.

Bei der Mod kann mal wählen, ob man die Daten nur in ein anderes Verzeichnis, welches nicht vom Backup erfasst wird verschieben, 
oder sofort permanent löschen möchte.

#### Wie steuert man den MOD?

Die Kommandos funktionieren NUR im Fraktionschat!

#### Hilfe

* \struct help : Zeigt die Kommandos der Mod an

#### Kommandos/Möglichkeiten

* \struct list => Informationen über die Strukturen und die möglichen Aufräumarbeiten
* \struct help => Liste der Kommandos
* \struct calc => Objekte erneut untersuchen und die Aufräummöglichkeiten ermitteln
* \struct cleanup => Das Aufräumen durchführen (ob dabei ein Verschieben oder Löschen stattfindet kann in der Konfiguration eingestellt werden)

### Konfiguration
Eine Konfiguration kann man in der Datei (wird beim ersten Start automatisch erstellt)

[Empyrion Directory]\Saves\Games\[SaveGameName]\Mods\EmpyrionStructureCleanUp\StructureCleanUpsDB.xml

vornehmen.

* OnlyCleanIfOlderThan: Nur Objekte aufräumen, die nicht mehr benutzt werden UND länger als N Tage nicht geändert wurden
* MoveToDirectory: Dateien in dieses Verzeichnis verschieben (Ausgangspunkt ist das EmpyrionStructureCleanUp-MOD-Datenverzeichnis)
Diese Option ist nur relevant, wenn DeletePermanent=false ist
* DeletePermanent: Dateien nicht verschieben sondern sofort permanent löschen
* CleanOnStartUp: Aufräumen beim Start der Mod (Serverstart) automatisch vornehmen 

### Was kommt noch?
Zunächst erstmal und damit viel Spaß beim Aufräumen wünscht euch

ASTIC/TC

***

English-Version:

---

You can find a translated version in the EmpyrionStructureCleanUp / bin directory if you do not want to check and compile the mod myself ;-)

Or here: https://empyriononline.com/threads/mod-empyrionstructurecleanup-in-shared-folder.45338/

### What is it for?

Empyrion lets all structures in the Shared directory stand even if they are no longer referenced in the game.
Thus, this directory grows larger and contains more and more subdirectories and files.
This affects the size and duration of a backup and also suffers the performance of file accesses
from a certain number. Likewise, the overview is lost because the files and directories are not
"see" if they are still used.

In the mod can sometimes choose whether you move the data only in another directory, which is not captured by the backup,
or want to permanently delete it immediately.

#### How to control the MOD?

The commands work ONLY in the fractional vote!

#### Help

* \struct help: Displays the commands of the mod

#### Commands / Options

* \struct list => Information about the structures and the possible cleanups
* \struct help => list of commands
* \struct calc => re-examine objects and find the cleanup options
* \struct cleanup => Perform the cleanup (whether a move or a delete takes place in the configuration)

### configuration
A configuration can be found in the file (automatically created on first startup)

[Empyrion Directory]\Saves\Games\[SaveGameName]\Mods\EmpyrionStructureCleanUp\StructureCleanUpsDB.xml

make.

* OnlyCleanIfOlderThan: Only clean objects that are no longer used AND have not been changed for more than N days
* MoveToDirectory: move files to this directory (starting point is the EmpyrionStructureCleanUp-MOD data directory)
This option is only relevant if DeletePermanent = false
* DeletePermanent: Do not copy files but immediately permanently delete them
* CleanOnStartUp: Clean up automatically when starting the mod (server start)

### What else is coming?
First of all, and have fun cleaning up you wish

ASTIC / TC