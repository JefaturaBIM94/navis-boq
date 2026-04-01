using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IQuantityMapperService
    {
        BoqRow ToBoqRow(ElementSnapshot snapshot);
    }
}