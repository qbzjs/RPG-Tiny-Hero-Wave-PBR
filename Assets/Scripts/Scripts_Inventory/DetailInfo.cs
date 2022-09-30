using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DetailInfo : MonoBehaviour
{
    TextMeshProUGUI itemName;
    TextMeshProUGUI itemPrice;

    private void Awake()
    {
        itemName = transform.Find("info_ItemName").GetComponent<TextMeshProUGUI>();
        itemPrice = transform.Find("info_Price").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void OnOff(bool b)
    {
        if(b)
        {
            gameObject.SetActive(true);

        }else
        {
            gameObject.SetActive(false);
        }
    }

    public void InfoSet(InventoryItem inventoryItem)
    {
        itemName.text = inventoryItem.itemName;
        itemPrice.text = $"{inventoryItem.itemData.Price}��";
    }
}
