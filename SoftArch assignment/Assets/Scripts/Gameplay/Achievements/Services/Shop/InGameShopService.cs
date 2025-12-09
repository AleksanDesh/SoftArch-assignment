using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameShopService : ShopService
{
    public override void PurchaseProduct(string productName)
    {
        shopText.text = "You've purchased " + productName + " with in game currency.";
    }
}
