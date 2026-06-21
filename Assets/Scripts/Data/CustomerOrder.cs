/// <summary>
/// 손님이 요청한 주문입니다. 암호형 대사와 목표 메뉴 정의를 담습니다.
/// Phase 5-R에서는 미리보기에 사용하고, Phase 6/7에서 손님과 연결됩니다.
/// </summary>
public class CustomerOrder
{
    public MenuDefinition menu;
    public string phrase = string.Empty;
    public string[] specialTags = System.Array.Empty<string>();

    public string MenuName => menu != null && !string.IsNullOrEmpty(menu.menuName) ? menu.menuName : "주문";
    public int BasePrice => menu != null ? menu.basePrice : 40;

    public static CustomerOrder FromMenu(MenuDefinition menu)
    {
        if (menu == null)
            return new CustomerOrder { phrase = "메뉴판에서 골라주세요" };

        return new CustomerOrder
        {
            menu = menu,
            phrase = menu.RandomPhrase(),
            specialTags = menu.specialTags ?? System.Array.Empty<string>()
        };
    }
}
