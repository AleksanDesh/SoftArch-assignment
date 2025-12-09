using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public void PuchaseProduct(string productName)
    {
        ServiceLocator.GetService<ShopService>().PurchaseProduct(productName);
    }
}
