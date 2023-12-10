using AudioSpectrumPlayer.Models;
using Caliburn.Micro;

namespace AudioSpectrumPlayer.ViewModels
{
    public class ShellViewModel : Screen
    {
        private string _firstName = "DING DONG";
        private string _lastName = "";
        private BindableCollection<PersonModel> _people = new();
        private PersonModel? _selectedPerson;

        public ShellViewModel()
        {
            People.Add(new PersonModel { FirstName = "Tim", LastName = "Corey" });
            People.Add(new PersonModel { FirstName = "Alice", LastName = "Example" });
            People.Add(new PersonModel { FirstName = "Bob", LastName = "Example2" });
        }

        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                _firstName = value;
                NotifyOfPropertyChange(() => FirstName);
                NotifyOfPropertyChange(() => FullName);
            }
        }

        public string LastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                _lastName = value;
                NotifyOfPropertyChange(() => LastName);
                NotifyOfPropertyChange(() => FullName);
            }
        }

        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }

        public BindableCollection<PersonModel> People
        {
            get { return _people; }
            set { _people = value; }
        }

        public PersonModel SelectedPerson
        {
            get
            {
                return _selectedPerson;
            }
            set
            {
                _selectedPerson = value;
                NotifyOfPropertyChange(() => SelectedPerson);
            }
        }

        public static bool CanClearText(string firstName, string lastName) => !string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName);

        public void ClearText(string firstName, string lastName)
        {
            FirstName = "";
            LastName = "";
        }

    }
}