using System.Collections.Generic; // 저장 원본 목록 사용
using UnityEngine; // 유니티 데이터 에셋 사용

[CreateAssetMenu(fileName = "SaveAssetCatalog", menuName = "Project C/Data/Save Asset Catalog")] // 저장 원본 목록 생성 메뉴
public sealed class SaveAssetCatalog : ScriptableObject // 저장 식별자 복원용 원본 목록
{ // 저장 원본 목록 시작
    [SerializeField] private List<CardData> cards = new List<CardData>(); // 복원 가능한 카드 목록
    [SerializeField] private List<RelicData> relics = new List<RelicData>(); // 복원 가능한 유물 목록
    [SerializeField] private List<ConsumableItemData> consumables = new List<ConsumableItemData>(); // 복원 가능한 소모품 목록
    public IReadOnlyList<CardData> Cards => cards; // 카드 원본 목록 조회
    public IReadOnlyList<RelicData> Relics => relics; // 유물 원본 목록 조회
    public IReadOnlyList<ConsumableItemData> Consumables => consumables; // 소모품 원본 목록 조회
} // 저장 원본 목록 종료
