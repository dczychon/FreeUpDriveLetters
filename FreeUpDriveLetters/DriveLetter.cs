using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;

namespace FreeUpDriveLetters
{
    /// <summary>
    /// Stellt einen Laufwerksbuchstaben dar
    /// </summary>
    class DriveLetter
    {
        private const string MountedDevicesRegKey = @"SYSTEM\MountedDevices";

        /// <summary>
        /// Systemkritische Laufwerksbuchstaben
        /// </summary>
        private static readonly string[] SystemCriticalLetters = { "c" };

        /// <summary>
        /// Laufwerksbuchstabe
        /// </summary>
        public char Letter { get; set; }

        /// <summary>
        /// Gibt an, ob dieser Laufwerksbuschstabe entfernt werden kann
        /// </summary>
        public bool CanBeRemoved { get; private set; }

        /// <summary>
        /// Wenn dieser Wert auf True gesetzt ist, wird dieser Laufwerksbuschstabe beim Clean aus der Registry gelöscht
        /// </summary>
        public bool MarkedForRemoval { get; set; } = false;

        /// <summary>
        /// Gibt den Namen, der Value in der Registry, für diesen <see cref="DriveLetter"/> zurück
        /// </summary>
        public string ExpectedRegistryValueName { get { return string.Format(@"\DosDevices\{0}:", Letter.ToString().ToUpper()); } }

        /// <summary>
        /// Ob der Laufwerksbuchstabe einen Eintrag in der Registry besitzt
        /// </summary>
        public bool Exists
        {
            get
            {
                try
                {
                    RegistryKey MountedDevices = GetMountedDevicesRegistryKey(false);
                    var content = MountedDevices.GetValue(ExpectedRegistryValueName);

                    if (content == null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gibt an, ob ein Gerät mit diesem Laufwerksbuchstaben gerade angeschlossen ist
        /// </summary>
        public bool CurrentlyMounted
        {
            get
            {
                return DriveLetterIsCurrentlyMounted(Letter);
            }
        }

        /// <summary>
        /// Gibt das Volume Label für diesen <see cref="DriveLetter"/> zurück
        /// </summary>
        public string DriveNameOrNull
        {
            get
            {
                try
                {
                    return GetDriveInfoForMountedDevice(this).VolumeLabel;
                }
                catch (IOException)
                {
                    //Gerät mit diesem Laufwerksbuchstaben ist nicht gemounted
                    return null;
                }
            }
        }

        /// <summary>
        /// Erstellt einen neuen <see cref="DriveLetter"/> mit dem übergbenen Buchstaben
        /// </summary>
        /// <param name="Drive">Laufwerksbuchstabe</param>
        public DriveLetter(char Drive)
        {
            Letter = Drive;

            if (IsProtectedLetter(Drive) || DriveLetterIsCurrentlyMounted(Drive))
            {
                //Der übergebene Laufwerksbuchstabe ist gearde von einem Gerät in verwendung oder Systemkritisch
                CanBeRemoved = false;
            }
            else
            {
                CanBeRemoved = true;
            }
        }

        /// <summary>
        /// Erstellt einen neuen <see cref="DriveLetter"/> und überschreibt die CanBeRemoved Eigenschaft
        /// </summary>
        /// <param name="Drive">Laufwerksbuschstabe</param>
        /// <param name="AlwaysAllowRemove">Ob dieser Laufwerksbuschstabe aus der Registry entfernt werden darf (VORSICHT!!)</param>
        public DriveLetter(char Drive, bool AlwaysAllowRemove) : this(Drive)
        {
            if (AlwaysAllowRemove)
            {
                if (CanBeRemoved == false)
                {
                    CanBeRemoved = true;
                }
            }
        }

        /// <summary>
        /// Löscht den Laufwerksbuchstaben aus der Registry
        /// </summary>
        /// <exception cref="InvalidOperationException">Wenn ein entfernen nicht erlaubt ist</exception>
        public void Delete()
        {
            if (CanBeRemoved)
            {
                DeleteFromRegistry(this, true);
            }
            else
            {
                throw new InvalidOperationException($"Der Laufwerksbuchstabe {Letter} kann nicht entfernt werden, weil er entweder gerade von einem angeschlossenen Gerät genutzt wird oder Systemkritisch ist");
            }
        }

        /// <summary>
        /// Gibt <see cref="DriveInfo"/> des Gerätes mit diesem Laufwerksbuchstaben zurück
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">Wenn der Laufwerksbuchstabe momentan nicht gemounted ist</exception>
        public DriveInfo GetDriveInfo()
        {
            return GetDriveInfoForMountedDevice(this);
        }


        #region "Statisches Zeug"
        /// <summary>
        /// Prüft ob der Laufwerksbuchstabe ein Systemkritischer ist
        /// </summary>
        /// <param name="Letter">Zu prüfender Buchstabe</param>
        /// <returns></returns>
        private static bool IsProtectedLetter(char Letter)
        {
            foreach (string pl in SystemCriticalLetters)
            {
                if (Letter.ToString().ToLower().Contains(pl))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Prüft ob der Laufwerksbuchstabe ein Systemkritischer ist
        /// </summary>
        /// <param name="Letter">Zu prüfender Buchstabe</param>
        /// <returns></returns>
        public static bool IsProtectedLetter(DriveLetter Letter)
        {
            return IsProtectedLetter(Letter.Letter);
        }

        /// <summary>
        /// Gibt die Reservierten Laufwerksbuchstaben aus der Registry zurück
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IEnumerable<DriveLetter> GetReservedFromRegistry(bool AlwaysAllowRemove = false)
        {
            List<DriveLetter> ReservedLetters = new List<DriveLetter>();

            //Öffnet den Key mit den reservierten Laufwerksbuchstaben
            RegistryKey MountedDevicesKey = GetMountedDevicesRegistryKey(false);

            //Loopt durch jede Value in dem RegistryKey
            foreach (string MountedDevice in MountedDevicesKey.GetValueNames())
            {
                if (IsValueNameReservedLetter(MountedDevice))
                {
                    //Die aktuelle Value ist ein Laufwerksbuchstabe
                    ReservedLetters.Add(new DriveLetter(ExtractDriveLetterFromValueName(MountedDevice), AlwaysAllowRemove));
                }
            }

            return ReservedLetters;
        }

        /// <summary>
        /// Prüft ob der übergebene Value Name ein Eintrag für einen Reservierten Laufwerksbuchstaben ist
        /// </summary>
        /// <param name="ValueName">Zu prüfender Value Name</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Wenn der Value Name null ist</exception>
        private static bool IsValueNameReservedLetter(string ValueName)
        {
            if (string.IsNullOrEmpty(ValueName))
            {
                throw new ArgumentNullException("ValueName");
            }

            return ValueName.ToLower().Contains(@"\dosdevices\");
        }

        /// <summary>
        /// Extrahiert aus einem Value Namen einen Drive Letter
        /// </summary>
        /// <param name="ValueName">Name aus dem extrahiert werden soll</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Wenn der Value Name nicht für einen Reservierten Laufwerksbuchstaben steht</exception>
        /// <exception cref="ArgumentNullException">Wenn der Value Name null ist</exception>
        private static char ExtractDriveLetterFromValueName(string ValueName)
        {
            if (string.IsNullOrEmpty(ValueName))
            {
                throw new ArgumentNullException("ValueName");
            }

            if (IsValueNameReservedLetter(ValueName) == false)
            {
                throw new ArgumentException("ValueName", $"Der übergebene Value Name '{ValueName}' steht nicht für einen reservierten Laufwerksbuchstaben");
            }
            
            return Convert.ToChar(ValueName.Substring(12, 1));
        }

        /// <summary>
        /// Löscht den übergebenen RegistryKey aus der Registry
        /// </summary>
        /// <param name="Letter">Zu löschender Laufwerksbuchstabe</param>
        /// <param name="force">Wenn true, wird er entfernt, auch wenn er gerade in Benutzung ist</param>
        /// <exception cref="InvalidOperationException">Wenn der Laufwerksbuchstabe gearde von einem Gerät verwendet wird</exception>
        public static void DeleteFromRegistry(DriveLetter Letter, bool force = false)
        {
            if (force == false)
            {
                if (DriveLetterIsCurrentlyMounted(Letter.Letter))
                {
                    throw new InvalidOperationException($"Der Laufwerksbuchstabe kann nicht aus der Registry entfernt werden, weil er gerade von einem Gerät verwendet wird");
                }
            }

            RegistryKey MountedDevicesKey = GetMountedDevicesRegistryKey(true);
            MountedDevicesKey.DeleteValue(Letter.ExpectedRegistryValueName);
        }

        /// <summary>
        /// Gibt den geöffneten MountedDevices Registry Key zurück
        /// </summary>
        /// <returns></returns>
        private static RegistryKey GetMountedDevicesRegistryKey(bool Writable)
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(MountedDevicesRegKey, Writable);
        }

        /// <summary>
        /// Gibt an, ob der übergebene Laufwerksbuchstabe einem momentan angeschlossenen Gerät zugeordnet ist
        /// </summary>
        /// <param name="Letter">Zu prüfender Laufwerksbuchstabe</param>
        /// <returns></returns>
        private static bool DriveLetterIsCurrentlyMounted(char Letter)
        {
            foreach (DriveInfo MountedDrive in DriveInfo.GetDrives())
            {
                if (MountedDrive.Name.Substring(0, 1).ToLower() == Letter.ToString().ToLower())
                {
                    //Der Laufwerksbuchstabe ist momentant in use
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gibt zurück wieviele inder der Liste aus der Registry entfernt werden könnten
        /// </summary>
        /// <param name="Letters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Letters ist null oder leer</exception>
        public static int AvailableForRemove(IEnumerable<DriveLetter> Letters)
        {
            if (Letters == null || Letters.Count() <= 0)
            {
                throw new ArgumentNullException("Letters");
            }

            int counter = 0;

            foreach (DriveLetter l in Letters)
            {
                if (l.CanBeRemoved)
                {
                    counter++;
                }
            }

            return counter;
        }

        /// <summary>
        /// Gibt <see cref="DriveInfo"/> für den übergebnen Laufwerksbuchstaben zurück
        /// </summary>
        /// <param name="Letter">Laufwerksbuchstabe</param>
        /// <returns></returns>
        /// <exception cref="IOException">Gerät mit dem übergebnenen Laufwerksbuchstaben ist nicht gemounted</exception>
        public static DriveInfo GetDriveInfoForMountedDevice(char Letter)
        {
            foreach (DriveInfo MountedDrive in DriveInfo.GetDrives())
            {
                if (MountedDrive.Name.Substring(0, 1).ToLower() == Letter.ToString().ToLower())
                {
                    return MountedDrive;
                }
            }

            //Wenn momentan kein Gerät mit dem Laufwerksbuchstaben angeschlossen ist
            throw new IOException($"Es ist kein Gerät mit dem Laufwerksbuchstaben {Letter} gemounted");
        }

        /// <summary>
        /// Gibt <see cref="DriveInfo"/> für den übergebnen Laufwerksbuchstaben zurück
        /// </summary>
        /// <param name="Letter">DriveLetter Information</param>
        /// <returns></returns>
        /// <exception cref="IOException">Gerät mit dem übergebnenen Laufwerksbuchstaben ist nicht gemounted</exception>
        public static DriveInfo GetDriveInfoForMountedDevice(DriveLetter Letter)
        {
            return GetDriveInfoForMountedDevice(Letter.Letter);
        }
        #endregion
    }
}
