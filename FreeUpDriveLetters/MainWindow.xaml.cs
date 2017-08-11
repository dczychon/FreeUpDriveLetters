using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

namespace FreeUpDriveLetters
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<DriveLetter> SystemDriveLetters;
        bool AlwaysAllowRemoval;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = string.Format("{0} - v.{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            this.PreviewKeyDown += HandleKeyKlick;

            AlwaysAllowRemoval = AllowForceDelete();

            if (AlwaysAllowRemoval)
            {
                MessageBox.Show("ACHTUNG!!\nWenn sie einen Laufwerksbuchstabe freigeben der gearde genutzt wird, oder Systemkritisch ist, kann das System in einen Instabilen Zustand versetzt werden!", "Vorsicht", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            btnFreeUpSelected.IsEnabled = false;
        }

        private void HandleKeyKlick(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                //Beenden wenn Esc gedrückt wurde
                Close();
            }
        }

        /// <summary>
        /// Prüft ob über den Startparameter angegeben wurde, ob jeder Laufwerksbuchstabe freigegeben werden kann
        /// </summary>
        /// <returns></returns>
        private bool AllowForceDelete()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length <= 1)
            {
                return false;
            }

            try
            {
                return args[1].ToLower() == "f";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SystemDriveLetters = DriveLetter.GetReservedFromRegistry(AlwaysAllowRemoval).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Es ist ein Fehler beim Laden der Laufwerksbuchstaben aufgetreten\n\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            DriveLetterList.ItemsSource = SystemDriveLetters;
        }

        private void btnFreeUpSelected_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sollen die ausgewählten Laufwerksbuchstaben freigegeben werden?", "Fortfahren", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                //Auswahl soll doch nicht entfernt werden
                return;
            }

            string FailedLetter = "";

            try
            {
                int counter = 0;
                foreach (DriveLetter letter in SystemDriveLetters)
                {
                    if (letter.MarkedForRemoval)
                    {
                        FailedLetter = letter.Letter.ToString();
                        letter.Delete();
                        counter++;
                    }
                }

                MessageBox.Show($"Die gewählten {counter} Laufwerksbuchstaben wurden freigegeben", "Erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Beim freigeben des Laufwerkbuchstabends {FailedLetter}, ist ein Fehler aufgetreten.\n\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        /// <summary>
        /// Ist mindestens ein Laufwerksbuchstabe zum entfernen ausgewählt?
        /// </summary>
        /// <returns></returns>
        private bool AtLeastOneValidChecked()
        {
            foreach (var item in DriveLetterList.Items)
            {
                if (item is DriveLetter)
                {
                    if ((item as DriveLetter).MarkedForRemoval)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ToRemoveSelectionChanged(object sender, RoutedEventArgs e)
        {
            btnFreeUpSelected.IsEnabled = AtLeastOneValidChecked();
        }
    }
}
