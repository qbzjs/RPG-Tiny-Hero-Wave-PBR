using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestItem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        ItemFactory.MakeItem(ItemIDCode.Diamond,transform.position,true);
        ItemFactory.MakeItem(ItemIDCode.Sword, transform.position, true);
        ItemFactory.MakeItem(ItemIDCode.Potion, transform.position, true);
        ItemFactory.MakeItem(ItemIDCode.Armor, transform.position, true);
        ItemFactory.MakeItem(ItemIDCode.Shield, transform.position, true);
        ItemFactory.MakeItem(ItemIDCode.Potion_Mana, transform.position, true);
        ItemFactory.MakeItem(ItemIDCode.Gold, transform.position, true);
    }

    
}
