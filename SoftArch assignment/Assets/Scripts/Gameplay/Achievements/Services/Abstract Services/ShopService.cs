using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public abstract class ShopService : MonoBehaviour
{
    [SerializeField]
    protected TextMeshProUGUI shopText;
    public abstract void PurchaseProduct(string productName);
}