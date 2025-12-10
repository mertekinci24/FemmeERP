namespace InventoryERP.Infrastructure.Services
{
    public record CompanyProfileDto(string Name, string Address, string Vkn, string Mersis);

    public interface ICompanyService
    {
        CompanyProfileDto GetCompanyProfile();
    }

    public class CompanyService : ICompanyService
    {
        public CompanyProfileDto GetCompanyProfile()
        {
            return new CompanyProfileDto(
                "FemmeStocks Teknoloji A.Ş.",
                "Merkez Mah. Teknoloji Cad. No:1 İstanbul",
                "1234567890",
                "012345678900001");
        }
    }
}
