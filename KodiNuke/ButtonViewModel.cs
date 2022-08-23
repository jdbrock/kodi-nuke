using PropertyChanged;
using System.Windows.Input;

namespace KodiNuke
{
    [ImplementPropertyChanged]
    public class ButtonViewModel
    {
        public string Text { get; set; }

        public ICommand Command { get; set; }

        public ButtonViewModel(string text, ICommand command)
        {
            Text = text;
            Command = command;
        }
    }
}