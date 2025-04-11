using Il2CppScheduleOne.Economy;

namespace EmployeeExtender.Utils
{
    internal class GameUtils
    {
        public static List<Dealer> GetAllDealers()
        {
            List<Dealer> dealers = UnityEngine.GameObject.FindObjectsOfType<Dealer>().ToList();
            return dealers;
        }

        public static List<Dealer> GetRecruitedDealers()
        {
            List<Dealer> dealers = UnityEngine.GameObject.FindObjectsOfType<Dealer>(true).ToList();
            dealers = dealers.Where(dealer => dealer.IsRecruited).ToList();
            return dealers;
        }


    }
}
