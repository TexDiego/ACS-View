using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ACS_View.MVVM.ViewModel
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;

        // Indica se o ViewModel está ocupado (útil para desabilitar comandos durante operações assíncronas)
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Evento de notificação de alteração de propriedade
        public event PropertyChangedEventHandler PropertyChanged;

        // Método para atualizar propriedades de forma genérica
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Notifica a interface de que uma propriedade foi alterada
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Método auxiliar para executar comandos assíncronos de forma segura
        protected async Task ExecuteAsync(Func<Task> action)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                await action();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
