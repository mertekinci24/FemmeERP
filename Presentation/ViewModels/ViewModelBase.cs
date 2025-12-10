// ReSharper disable once All
#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace InventoryERP.Presentation.ViewModels
{
  public abstract class ViewModelBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
    protected bool SetProperty<T>(ref T f, T v, [CallerMemberName] string? n = null)
    {
      if (EqualityComparer<T>.Default.Equals(f, v)) return false;
      f = v;
      OnPropertyChanged(n);
      return true;
    }
  }
}

