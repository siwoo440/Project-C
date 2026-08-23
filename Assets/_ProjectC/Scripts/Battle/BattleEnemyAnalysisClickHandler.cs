using UnityEngine; // 전투 유닛 화면 기능 사용
using UnityEngine.EventSystems; // 적 클릭 이벤트 사용

public sealed class BattleEnemyAnalysisClickHandler :
    MonoBehaviour,
    IPointerClickHandler // 적 본체 클릭 정보 표시 처리기
{
    private BattleUnitView unitView; // 연결된 적 유닛 화면

    public void Initialize(
        BattleUnitView targetUnitView) // 적 유닛 화면 연결
    {
        unitView = targetUnitView; // 적 화면 저장
    }

    private void Awake() // 적 화면 자동 조회
    {
        unitView =
            GetComponent<BattleUnitView>(); // 같은 오브젝트 적 화면 조회
    }

    public void OnPointerClick(
        PointerEventData eventData) // 적 본체 좌클릭 처리
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            unitView == null ||
            unitView.RuntimeUnit == null ||
            unitView.RuntimeUnit.Team != BattleTeam.Enemy)
        {
            return;
        }

        Canvas battleCanvas =
            unitView.GetComponentInParent<Canvas>(); // 전투 Canvas 조회

        BattleEnemyAnalysisView analysisView =
            BattleEnemyAnalysisView.EnsureInstance(
                battleCanvas); // 적 정보 패널 준비

        if (analysisView == null)
        {
            return;
        }

        analysisView.Toggle(
            unitView); // 적 클릭 정보 표시·해제
    }
}
