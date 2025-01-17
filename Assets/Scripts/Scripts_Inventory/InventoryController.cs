using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour
{
    //[HideInInspector]
    private ItemGrid selectedItemGrid; //현재 선택된 인벤토리를 할당하는 변수

    /// <summary>
    /// 현재 할당된 인벤토리를 넘겨주는 프로퍼티 
    /// set이 될때 inventoryHighlight(아이템위에 마우스를 올리거나 선택하면 주변을 하얗게 해주는 오브젝트)를 자식으로 만듬
    /// inventoryHighlight를 자식으로 만들어야 다른 인벤토리에 가려서 안보이지 않는다.
    /// </summary>
    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);

        }
    }

    EquipSlot selectedEquipSlot;

    public EquipSlot SelectedEquipSlot
    {
        get => selectedEquipSlot;
        set
        {
            selectedEquipSlot = value;
        }
    }



    InventoryItem selectedItem; //현재 선택된 아이템 변수
    InventoryItem overlapItem; // 아이템을 바꿔줄때 잠시 넣어줄 변수
    RectTransform rectTransform; //아이템의 캔버스위치를 받아올 변수
    RectTransform detailRectTrensform; // 아이템 디테일창을 위치를 미리찾아둘 변수


    //[SerializeField] List<ItemData> items; //사용할 아이템 데이터 !! 데이터매니저를 사용해서 이제 사용안함
    [SerializeField] GameObject itemPrefab; //생성할 아이템의 프리펩
    [SerializeField] Transform canvasTransform; // 인벤토리를 보여줄 캔버스를 넣는 변수
    [SerializeField] DetailInfo detailInfo; //아이템의 정보를 보여줄 스크립트
    [SerializeField] FailButton buyFailButton;  //돈이 없을때 나타나는 메시지
    [SerializeField] FailButton inventoryFailButton; // 인벤토리에 자리가 없을때 나타나는 메시지
    
    
    BuyQustion buyQustion; // 아이템 구매시 나타날 오브젝트

    Vector2Int oldPosition;
    InventoryItem itemToHighlight;
    InventoryHighlight inventoryHighlight;

    PlayerInput inputActions;

    FindPlayerInventory playerInventory;
    ItemGrid playerItemGrid;
    EquipControl playerEquipControl;
    Player player;
    InventoryGold playerGold; // 플레이어 골드를 표시해주는 스크립트

    ItemGrid tempNPCInventory; // NPC거래용 인벤토리 저장할 변수
    InventoryItem tempNPCItem; // NPC거래용 인벤토리 아이템 변수
    Vector2Int tempNPCPosition; // NPC거래용 인벤토리아이템 원래 위치 저장할 변수

    public InventoryItem SelectedItem
    {
        get { return selectedItem; }
        set { selectedItem = value; }
    }
    private void Awake()
    {
        inputActions=new PlayerInput();
        inventoryHighlight=GetComponent<InventoryHighlight>();
        detailRectTrensform = detailInfo.gameObject.GetComponent<RectTransform>();
        playerInventory=FindObjectOfType<FindPlayerInventory>();
        playerEquipControl=FindObjectOfType<EquipControl>();
        playerItemGrid=playerInventory.GetComponent<ItemGrid>();
        player = FindObjectOfType<Player>();
        buyQustion = FindObjectOfType<BuyQustion>();
        

    }

    private void OnEnable()
    {
        inputActions.InventoryUI.Enable();
        inputActions.InventoryUI.InventoryOnOff.performed += OnInventoryOnOff;
        inputActions.InventoryUI.CreateItem.performed += OnCreateItem;
        inputActions.InventoryUI.CreateEmptyItem.performed += OnCreateEmptyItem;
        inputActions.InventoryUI.ItemRotate.performed += OnItemRotate;
        inputActions.InventoryUI.LeftClick.performed += OnLeftClick;
        inputActions.InventoryUI.RightClick.performed += OnRightClick;
    }

    private void OnDisable()
    {
        inputActions.InventoryUI.RightClick.performed -= OnRightClick;
        inputActions.InventoryUI.LeftClick.performed -= OnLeftClick;
        inputActions.InventoryUI.ItemRotate.performed -= OnItemRotate;
        inputActions.InventoryUI.CreateEmptyItem.performed -= OnCreateEmptyItem;
        inputActions.InventoryUI.CreateItem.performed -= OnCreateItem;
        inputActions.InventoryUI.InventoryOnOff.performed -= OnInventoryOnOff;
        inputActions.InventoryUI.Disable();
    }
    private void Start()
    {
        buyQustion.YesButton.onClick.AddListener(BuyOk);
        buyQustion.NoButton.onClick.AddListener(BuyCancel);
        buyQustion.gameObject.SetActive(false);
        playerGold = FindObjectOfType<InventoryGold>();
        playerItemGrid.onChangeGold += (gold) =>
        {
            playerGold.GoldChange(gold);
        };

        playerItemGrid.Gold = 0;


    }
    private void OnInventoryOnOff(InputAction.CallbackContext obj)
    {
        //인벤토리가 off될때 선택하고 있는 인벤토리할당해제 인벤토리 끌때 아이템 들고있으면 들고있는 아이템 부모변경
        if (!playerInventory.OnOff())
        {
            if(selectedItem!=null)
            {
                rectTransform.SetParent(canvasTransform);
            }

            selectedItemGrid = null;
            detailInfo.OnOff(false);
        }
        if (!playerEquipControl.OnOff())
        {
           selectedEquipSlot = null;
        }

    }

    public void OnOffInventory()
    {
        playerInventory.OnInventory();
        playerEquipControl.OnInventory();
        
    }

    private void OnCreateItem(InputAction.CallbackContext obj)
    {
        //들고있는 아이템이 없을때만 q를 눌렀을때 아이템생성
        if (selectedItem == null)
        {
            CreateRandomItem();
        }
        
    }

    private void OnCreateEmptyItem(InputAction.CallbackContext obj)
    {
        //인벤토리의 빈칸에다가 아이템 넣기
        InsertRandomItem();
    }

    private void OnItemRotate(InputAction.CallbackContext obj)
    {

        //아이템 90도 돌리기 이미 돌려져 있다면 다시돌아옴

        RetateItem();
    }

    private void OnLeftClick(InputAction.CallbackContext obj)
    {
        //인벤토리가 켜져있을때만 실행
        if (selectedItemGrid != null || selectedEquipSlot!=null)
        {
            LeftMouseButtonPress();
        }
    }


    private void OnRightClick(InputAction.CallbackContext obj)
    {
        if(selectedItemGrid!=null)
        {
            RightMouseButtonPress();
        }
    }


    private void Update()
    {
        ItemIconDrag();
        DetailDrag();

        if (selectedEquipSlot != null)
        {
            if (selectedEquipSlot.SlotItem != null && selectedItem==null)
            {
                detailInfo.OnOff(true);
                detailInfo.InfoSet(selectedEquipSlot.SlotItem);
            }


        }
        //선택한 아이템이 없다면 하이라이트 보여주지않음
        if (selectedItemGrid == null && selectedEquipSlot==null)
        {
            inventoryHighlight.Show(false);
            detailInfo.OnOff(false);


            return;
        }

        if (selectedItemGrid != null)
        {
            HandleHighlight();
        }
        

        

     

        


    }

    /// <summary>
    /// detailInfo창을 움직이는함수
    /// </summary>
    private void DetailDrag()
    {
        if (detailInfo.gameObject.activeSelf)// detail창이 존재할때만
        {
            Vector2 mousePos = Mouse.current.position.ReadValue(); //마우스움직임받기
            Vector2 pivotVector = new Vector2(-2.0f, -3.0f); // 기본 피봇위치

            
            RectTransform rect = (RectTransform)detailRectTrensform.transform; // detail창의 위치 저장
            if ((mousePos.x + rect.sizeDelta.x * 3.5f) > Screen.width) //스크린옆으로 나갔을때
            {
                pivotVector.x = 3.0f;
            }
            if ((mousePos.y + rect.sizeDelta.y * 5.5f) > Screen.height) // 스크린위로 나갔을때
            {
                pivotVector.y = 3.0f;
            }
            


            detailRectTrensform.pivot = pivotVector; //최종피봇설정
            detailRectTrensform.transform.position = mousePos; // 마우스따라다니게 설정


        }
    }

    /// <summary>
    /// 아이템을 돌리는 함수,
    /// 들고있는 아이템이 없다면 return
    /// 그렇지 않으면 InventoryItem스크립트 안에있는 Rotate함수 실행
    /// </summary>
    private void RetateItem()
    {
        if (selectedItem == null)
        {
            return;
        }

        selectedItem.Rotate();
        Vector2Int positiononGrid = GetTileGridPosition();
        inventoryHighlight.SetSize(selectedItem);
        inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positiononGrid.x, positiononGrid.y);

    }

    /// <summary>
    /// 인벤토리 빈공간에 아이템을 넣는 함수 빈공간이 없으면 삭제한다.
    /// </summary>
    private void InsertRandomItem()
    {
        if(selectedItemGrid==null)
        {
            return;
        }
        if(selectedItem!=null)
        {
            return;
        }

        CreateRandomItem();
        InventoryItem itemToInsert = selectedItem;
        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceforObject(itemToInsert);
        if(posOnGrid==null)
        {
            Destroy(selectedItem.gameObject);
            return;
        }
        selectedItem = null;
        InsertItem(itemToInsert);
    }

    public void InsertRandomItem(ItemGrid temp,ItemIDCode item)
    {
        if(temp==null)
        {
            return;
        }
        if (selectedItem != null)
        {
            return;
        }

        CreateRandomItem((uint)item);
        InventoryItem itemToInsert = selectedItem;
        Vector2Int? posOnGrid = temp.FindSpaceforObject(itemToInsert);
        if (posOnGrid == null)
        {
            Destroy(selectedItem.gameObject);
            return;
        }
        selectedItem = null;
        InsertItem(itemToInsert,temp);
    }



    /// <summary>
    /// 아이템을 인벤토리에 넣는 함수,
    /// Nullable Type으로 만든 FindSpaceforObject함수로 인벤토리 안에 들어갈 수 있는지 확인하고
    /// 값이 null이면 리턴 아니면 인벤토리에 넣어준다.
    /// </summary>
    /// <param name="itemToInsert">들어갈 아이템 정보</param>
    private void InsertItem(InventoryItem itemToInsert)
    {
        


        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceforObject(itemToInsert);

        if(posOnGrid==null)
        {
            return;
        }

        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    private void InsertItem(InventoryItem itemToInsert,ItemGrid Inventory)
    {
        Vector2Int? posOnGrid = Inventory.FindSpaceforObject(itemToInsert);

        if (posOnGrid == null)
        {
            return;
        }

        Inventory.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    /// <summary>
    /// 아이템위에 마우스를 올려놓거나 아이템을 선택했을때 그부분을 하이라이트해주는 함수
    /// </summary>
    private void HandleHighlight()
    {
        Vector2Int positiononGrid = GetTileGridPosition();
        if(oldPosition==positiononGrid) //선택한 위치와 예전위치가 같으면 리턴
        {
            return;
        }

        oldPosition = positiononGrid; //선택한 위치를 올드포지션에 넣어줌
                                      


        if (selectedItem == null) //들고있는 아이템이 없을때
        {
            itemToHighlight = selectedItemGrid.GetItem(positiononGrid.x, positiononGrid.y);
            

            if(itemToHighlight!=null)
            {
                inventoryHighlight.Show(true);
                detailInfo.OnOff(true);
                detailInfo.InfoSet(itemToHighlight);

                inventoryHighlight.SetSize(itemToHighlight);
                //inventoryHighlight.SetParent(selectedItemGrid);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
            }else
            {
                detailInfo.OnOff(false);
                inventoryHighlight.Show(false);
            }           
        }else //들고있는 아이템 있을때
        {
            detailInfo.OnOff(false);
            //아이템이 들어가야할 자리에 하이라이트를 보여준다
            inventoryHighlight.Show(selectedItemGrid.BoundryCheck(positiononGrid.x,
                                                                   positiononGrid.y,
                                                                   selectedItem.WIDTH,
                                                                   selectedItem.HEIGHT));

            inventoryHighlight.SetSize(selectedItem);
            //inventoryHighlight.SetParent(selectedItemGrid);
            inventoryHighlight.SetPosition(selectedItemGrid,selectedItem,positiononGrid.x,positiononGrid.y);
        }
    }

    /// <summary>
    /// 랜덤한 아이템을 만들어 주는 함수,
    /// </summary>
    private void CreateRandomItem()
    {
        InventoryItem inventoryItem=Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;



        rectTransform=inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        //int selectedItemID=UnityEngine.Random.Range(0,items.Count);
        int selectedItemID=UnityEngine.Random.Range(0,DataManager.Instance.Items.Length-1);
        //inventoryItem.Set(items[selectedItemID]);
        inventoryItem.Set(DataManager.Instance.Items[selectedItemID]);


    }

    private void CreateRandomItem(uint itemCode)
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;



        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        //int selectedItemID=UnityEngine.Random.Range(0,items.Count);
        //int selectedItemID = UnityEngine.Random.Range(0, DataManager.Instance.Items.Length - 1);
        //inventoryItem.Set(items[selectedItemID]);
        inventoryItem.Set(DataManager.Instance.Items[itemCode]);


    }

    public bool PickUpItem(uint code,ItemGrid inventory)
    {
        bool result = false;
        if (selectedItem == null)
        {
            InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
            selectedItem = inventoryItem;

            rectTransform = inventoryItem.GetComponent<RectTransform>();
            rectTransform.SetParent(canvasTransform);
            rectTransform.SetAsLastSibling();

            inventoryItem.Set(DataManager.Instance.Items[code]);


            InventoryItem itemToInsert = selectedItem;
            Vector2Int? posOnGrid = inventory.FindSpaceforObject(itemToInsert);
            if(playerInventory.gameObject.activeSelf)
            {
                //인벤토리가 켜져있을때는 인벤토리에 넣지않고 손에들고 있게 하기 위한 if문
            }
            else if (posOnGrid != null)
            {
                //인벤토리에 자리가 있으면 아이템을 넣는다
                selectedItem = null;
                InsertItem(itemToInsert,inventory);
                //Destroy(selectedItem.gameObject);
                //return;
            }else
            {
                //인벤토리에 자리가 없으면 손에 들고있는 상태로 인벤토리를 킨다.
                playerInventory.OnInventory();
                playerEquipControl.OnInventory();
            }
          
            result = true;
        }
        return result;

    }

    /// <summary>
    /// 아이템 아이콘이 마우스를 쫓아 다니게 하는 함수
    /// </summary>
    private void ItemIconDrag()
    {
        if (selectedItem != null && rectTransform!=null)
        {
            if (!selectedItem.rotated)
            {
                //rectTransform.position = Input.mousePosition;
                rectTransform.position = Mouse.current.position.ReadValue();
            }else
            {
                rectTransform.position = Mouse.current.position.ReadValue();
                //rectTransform.position=new Vector3(Input.mousePosition.x,Input.mousePosition.y-(ItemGrid.tileSizeHeight*selectedItem.HEIGHT),0);
                rectTransform.position = new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y - (ItemGrid.tileSizeHeight * selectedItem.HEIGHT), 0);
            }

        }
    }


    private void RightMouseButtonPress()
    {
        if(selectedItem == null && selectedItemGrid!=null ) // 들고있는 아이템이 없고 선택중인 인벤토리가 있을때
        {
            if (selectedItemGrid.type == InventoryType.Player)
            {
                Vector2Int tileGridPosition = GetTileGridPosition();
                UseItem(tileGridPosition);
            }
        }
    }


    /// <summary>
    /// 마우스위치를 통해 몇번째 타일을 선택했는지 확인하고 현재 들고있는 아이템이 없다면 그냥들고
    /// 아이템이 있다면 PlaceItem을 통해 아이템을 교채해준다.
    /// 마우스위치를 통해 장비슬롯을 선택했는지 확인하고 여러가지 경우의수를 확인하고 장비를 장착할것인지 안할것인지 결정
    /// </summary>
    private void LeftMouseButtonPress()
    {

        //아이템기본 이동 및 상점 구매 판매
        if (selectedItem == null && selectedItemGrid != null) //들고있는 아이템이 없고 선택중인 인벤토리가 있을때
        {
            if (selectedItemGrid.type == InventoryType.NPC) //선택한 인벤토리 주인이 NPC일때
            {

                tempNPCInventory = selectedItemGrid;
                tempNPCPosition = GetTileGridPosition(tempNPCInventory);
                tempNPCItem = tempNPCInventory.PickItem(tempNPCPosition.x,tempNPCPosition.y); // NPC 인벤토리에서 아이템 정보를 받아옴
                if(tempNPCItem != null) // 아이템이 있다면 살건지 안살건지 물어보는 창을 연다.
                {
                    buyQustion.gameObject.SetActive(true);
                    buyQustion.infoSet(tempNPCItem.itemData);
                    buyQustion.gameObject.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
                }

            }
            else if (selectedItemGrid.type == InventoryType.Player)
            {
                Vector2Int tileGridPosition = GetTileGridPosition();
                PickUpItem(tileGridPosition);
                Vector2Int positiononGrid = GetTileGridPosition();
                inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positiononGrid.x, positiononGrid.y);
                detailInfo.OnOff(false);
            }

        }
        else if (selectedItem != null && selectedItemGrid != null) // 들고있는 아이템이 있고 선택중인 인벤토리가 있을때
        {
            if (selectedItemGrid.type == InventoryType.NPC) //선택한 인벤토리 주인이 NPC일때
            {
                //아이템 팔기
                Vector2Int? posOnGrid = selectedItemGrid.FindSpaceforObject(selectedItem);

                if(posOnGrid!=null)
                {
                    playerItemGrid.Gold += selectedItem.itemData.Price;

                    InsertItem(selectedItem);
                    selectedItem = null;
                }else
                {
                    playerItemGrid.Gold += selectedItem.itemData.Price;
                    Destroy(selectedItem.gameObject);
                    selectedItem = null;
                }

               
                //Vector2Int tileGridPosition = GetTileGridPosition();
                //PlaceItem(tileGridPosition);
            }
            else if(selectedItemGrid.type==InventoryType.Player)
            {
                //아이템 바꾸기
               
                Vector2Int tileGridPosition = GetTileGridPosition();
                PlaceItem(tileGridPosition);
            }

        }


        //장비슬롯에 아이템 장비할때
        if (selectedEquipSlot!=null)
        {
            if (selectedEquipSlot.SlotItem == null && selectedItem != null) //장비슬롯에 아이템이없고 아이템을 들고있을때
            {
                EquipSlot shieldCheck = playerEquipControl.FindEquipShield(); // 장비슬롯중에 실드가있는지 확인한다.

                if (shieldCheck == null) // 실드가 없으면 슬롯에 장착
                {
                    if (selectedEquipSlot.PlaceItem(selectedItem))
                    {
                        selectedItem = null;
                    }
                }else //실드가 있을때
                {
                    if (selectedEquipSlot.PlaceItem(selectedItem))
                    {
                        if (selectedItem.itemData.weaponType == WeaponType.Shield) // 현재 들고있는 아이템이 실드일때
                        {
                            //다른슬롯에 있는 실드를 손에들고 슬롯을 빈슬롯으로 만듬
                            InventoryItem tempSlotItem = shieldCheck.SlotItem; 
                            EquipSlotPlaceItem(tempSlotItem);
                            shieldCheck.SlotItem = null;
                        }else
                        {
                            //현재 들고있는 아이템이 실드가 아니면 그냥 장착
                            selectedItem = null;
                        }
                    }
                }
            } else if (selectedEquipSlot.SlotItem != null && selectedItem == null) // 장비슬롯에 아이템이있고 들고있는 아이템이 없을때
            {
                InventoryItem tempSlotItem = selectedEquipSlot.SlotItem;
                EquipSlotPlaceItem(tempSlotItem);
                selectedEquipSlot.SlotItem = null;
               
            }
            else if (selectedEquipSlot.SlotItem != null && selectedItem != null) // 장비슬롯에 아이템이있고 들고있는 아이템이 있을때
            {
                InventoryItem tempSlotItem = selectedEquipSlot.SlotItem;
                EquipSlot shieldCheck = playerEquipControl.FindEquipShield(); // 장비하고 있는 실드가 있는지 확인
                if (shieldCheck == null) // 실드가 없으면 대상 장비하고 스왑
                {
                    if (selectedEquipSlot.PlaceItem(selectedItem))
                    {
                        //Debug.Log("성공");
                        EquipSlotPlaceItem(tempSlotItem);
                        //selectedItem = null;

                    }
                }else //실드가 있을때
                {
                    //현재슬롯에 실드이면 있으면 장비스왑
                    if (selectedEquipSlot.SlotItem.itemData.weaponType == WeaponType.Shield)
                    {
                        if (selectedEquipSlot.PlaceItem(selectedItem))
                        {
                            EquipSlotPlaceItem(tempSlotItem);
                        }
                    } else if (selectedEquipSlot.SlotItem.itemData.weaponType != WeaponType.Armor && selectedItem.itemData.weaponType==WeaponType.Shield)
                    {
                        //현재슬롯이 실드가 아니라면 장비를 스왑하고 다른칸에 있는 아이템은 인벤토리로 간다.
                        if(selectedEquipSlot.PlaceItem(selectedItem))
                        {
                            
                            InventoryItem tempSlotItemshiled = shieldCheck.SlotItem;
                            RectTransform r= tempSlotItemshiled.GetComponent<RectTransform>();
                            r.pivot = pivot;
                            r.anchorMax = pivot;
                            r.anchorMin = pivot;
                            InsertItem(tempSlotItemshiled,playerItemGrid);
                            shieldCheck.SlotItem = null;
                            EquipSlotPlaceItem(tempSlotItem);

                        }
                    }
                }
                
            }
            
        }
    }

   

    Vector2 pivot = new Vector2(0, 1.0f);
    private void EquipSlotPlaceItem(InventoryItem slotItem)
    {
       
        selectedItem = slotItem;
        rectTransform = selectedItem.GetComponent<RectTransform>();
        rectTransform.SetParent(playerInventory.transform);
        rectTransform.pivot = pivot;
        rectTransform.anchorMax = pivot;
        rectTransform.anchorMin = pivot;
        rectTransform.SetAsLastSibling();
    }

    /// <summary>
    /// 현재 마우스위치가 인벤토리의 어느 칸에 있는지 반환해주는 함수
    /// </summary>
    /// <returns>인벤토리의 위치값 반환</returns>
    private Vector2Int GetTileGridPosition()
    {
        //Vector2 position = Input.mousePosition;
        Vector2 position = Mouse.current.position.ReadValue();

        //if(selectedItem!= null)
        //{
        //    position.x -= (selectedItem.itemData.width - 1) * ItemGrid.tileSizeWidth/2;
        //    position.y +=(selectedItem.itemData.height - 1) * ItemGrid.tileSizeHeight/2;
        //}

        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(position);
        return tileGridPosition;
    }

    private Vector2Int GetTileGridPosition(ItemGrid tempInventory)
    {
        //Vector2 position = Input.mousePosition;
        Vector2 position = Mouse.current.position.ReadValue();

        //if(selectedItem!= null)
        //{
        //    position.x -= (selectedItem.itemData.width - 1) * ItemGrid.tileSizeWidth/2;
        //    position.y +=(selectedItem.itemData.height - 1) * ItemGrid.tileSizeHeight/2;
        //}

        Vector2Int tileGridPosition = tempInventory.GetTileGridPosition(position);
        return tileGridPosition;
    }

    /// <summary>
    /// 아이템을 인벤토리에 넣어주는 함수,
    /// 먼저 인벤토리에 들어갈수 있는 아이템인지 확인하고
    /// 들어갈수 있는 아이템이면 넣어주고 다른아이템이 있다면 교체한다. 
    /// </summary>
    /// <param name="tileGridPosition">넣을려고 하는 인벤토리의 위치</param>
    private void PlaceItem(Vector2Int tileGridPosition)
    {
        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y,ref overlapItem);
        if(complete)
        {
            detailInfo.OnOff(true); //디테일창 활성화
            detailInfo.InfoSet(selectedItem); // 디테일창 정보 세팅
            selectedItem = null;
            if(overlapItem!=null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform=selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
            }
        }
        
        
       
    }

    /// <summary>
    /// 인벤토리에 있는 아이템을 selectedItem에 넣어주는 함수
    /// </summary>
    /// <param name="tileGridPosition">타일의 위치</param>
    private void PickUpItem(Vector2Int tileGridPosition)
    {
        

        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();
            rectTransform.SetAsLastSibling();
        }

    }

    private void UseItem(Vector2Int tileGridPosition)
    {
        

        InventoryItem useItem = selectedItemGrid.UseInventoryItem(tileGridPosition.x, tileGridPosition.y);
        if(useItem != null)
        {
            useItem.UseItem(player.GetComponent<IBattle>());
            Destroy(useItem.gameObject);
            //Debug.Log("사용할 아이템 있음");
        }
    }

    private void BuyOk()
    {
      
        Vector2Int? posOnGrid = playerItemGrid.FindSpaceforObject(tempNPCItem); // 플레이어 인벤토리에 자리가 있는지 본다.
        if (posOnGrid != null) //자리가 있을때
        {
            if(tempNPCItem.itemData.Price>playerItemGrid.Gold) //돈이 없을때
            {
                buyFailButton.gameObject.SetActive(true);
                buyQustion.gameObject.SetActive(false);
            }else
            {
                playerItemGrid.Gold -= tempNPCItem.itemData.Price;
                tempNPCItem = tempNPCInventory.PickUpItem(tempNPCPosition.x, tempNPCPosition.y); //자리가 있으면 인벤토리정보에서 뺀다. 
                InsertItem(tempNPCItem, playerItemGrid); // 플레이어 인벤토리에 넘겨주기
                buyQustion.gameObject.SetActive(false);
                //Destroy(tempNPCItem.gameObject);
                tempNPCInventory = null;
                tempNPCItem = null;
                tempNPCPosition = Vector2Int.zero;
            }


            

        }
        else  //자리가 없을때
        {
            inventoryFailButton.gameObject.SetActive(true);
            buyQustion.gameObject.SetActive(false);
            Debug.Log("자리가 없음");
        }
    }


    private void BuyCancel()
    {
        buyQustion.gameObject.SetActive(false);
    }
}
