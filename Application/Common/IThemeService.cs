namespace InventoryERP.Application.Common;

public interface IThemeService
{
    string Current { get; }
    void Set(string theme);
    void Toggle();
}
