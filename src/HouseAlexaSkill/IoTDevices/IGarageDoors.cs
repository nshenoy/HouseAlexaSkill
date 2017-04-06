using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouseAlexaSkill.IoTDevices
{
    public interface IGarageDoors
    {
        Task<string> GetSingleDoorStatus(string doorIdentifier);
        Task<Dictionary<string, string>> GetAllDoorsStatus();
        Task<string> ToggleGarageDoor(string doorAction, string doorIdentifier);
    }
}
