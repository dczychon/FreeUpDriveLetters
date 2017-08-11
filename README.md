# FreeUpDriveLetters

Wenn man unter Windows einen USB Stick oder andere Laufwerke an den Rechner anschließt, wird diesem Gerät ein Laufwerksbuchstabe zugewiesen, über den man auf das Gerät zugreifen kann.
Der zugewiesene Laufwerksbuchstabe ist dann für dieses Gerät reserviert und wird auch nach dem entfernen des Gerätes, vorerst nicht wieder freigegeben, so dass man ihn für ein anderes Laufwerk nutzen könnte.

Dieses Programm ermöglichgt die freigabe dieser reserviereten Laufwerksbuchstaben, so dass diese wieder genutzt werden können.



### Startparameter

Das Programm bietet standardmäßig nur Laufwerksbuchstaben zur freigabe an, die entweder gearde nicht verwendet werden, oder nicht Systemkritisch sind. Dadurch wird sichergestellt, dass ein entfernen des Laufwerksbuchstabens, keine negativen Effekte auf das System hat. Dieses verhalten kann über den Startparameter `f` deaktiviert werden.

Dazu startet sie das Programm über die Kommandozeile wiefolgt: `FreeUpDriveLetters.exe f`
