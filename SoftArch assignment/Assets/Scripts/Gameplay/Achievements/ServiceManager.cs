using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ServiceManager : MonoBehaviour
{
    private ShopService shopService;
    private AchievementService achievementService;
    [SerializeField]
    private bool isIOSBuild = false;

    //Unregister current services and then locate new services
    public void LocateServices()
    {
        UnregisterServices();
        if (isIOSBuild)
        {
            shopService = GetComponent<IOSAppStoreShopService>();
            achievementService = GetComponent<IOSGameCenterAchievementService>();
        }
        else
        {
            shopService = GetComponent<InGameShopService>();
            achievementService = GetComponent<SteamAchievementSystem>();
        }
        ServiceLocator.RegisterService<ShopService>(shopService);
        ServiceLocator.RegisterService<AchievementService>(achievementService);
    }

    public void UnregisterServices()
    {
        ServiceLocator.UnregisterService<ShopService>(shopService);
        ServiceLocator.UnregisterService<AchievementService>(achievementService);
    }

    //Locate services again if making changes in the Inspector(to the value of isIOSBuild)
    private void OnValidate()
    {
        LocateServices();
    }
}
