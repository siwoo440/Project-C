using System.Collections.Generic; // 적 예정 행동 목록 사용
using System.Text; // 여러 행동 순번 연결 사용
using TMPro; // 행동 순번 글자 표시 사용
using UnityEngine; // 적 화면 객체와 좌표 사용
using UnityEngine.UI; // 순번 배경 이미지 사용
public sealed class BattleEnemyActionOrderView : MonoBehaviour // 적 머리 위 행동 순번 표시
{ // 행동 순번 화면 시작
    private TMP_Text orderText; // 실제 행동 순번 글자
    private readonly StringBuilder builder = new StringBuilder(); // 여러 행동 순번 작성 도구
    public static BattleEnemyActionOrderView Create(Transform actor) // 적 머리 위 행동 순번 생성
    { // 행동 순번 화면 생성 시작
        GameObject root = new GameObject("EnemyActionOrder", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image), typeof(BattleEnemyActionOrderView)); // 적 행동 순번 화면 생성
        root.transform.SetParent(actor, false); // 실제 적 화면에 순번 연결
        Canvas layer = root.GetComponent<Canvas>(); // 독립 행동 순번 정렬 화면 조회
        layer.overrideSorting = true; // 화살표와 독립된 정렬 설정
        layer.sortingOrder = 411; // 행동 화살표보다 앞에 숫자 표시
        RectTransform area = root.transform as RectTransform; // 순번 화면 영역 조회
        area.anchorMin = new Vector2(0.5f, 1f); // 적 위쪽 중앙 최소 앵커
        area.anchorMax = new Vector2(0.5f, 1f); // 적 위쪽 중앙 최대 앵커
        area.pivot = new Vector2(0.5f, 0.5f); // 순번 중앙 기준점 설정
        area.anchoredPosition = new Vector2(0f, -15f); // 행동 예고 바로 위의 행동 순번 위치
        area.sizeDelta = new Vector2(100f, 27f); // 행동 순번 화면 크기 설정
        Image background = root.GetComponent<Image>(); // 행동 순번 배경 조회
        background.color = new Color(0.04f, 0.04f, 0.05f, 0.8f); // 숫자 가독성을 위한 어두운 배경
        background.raycastTarget = false; // 적과 카드 입력 차단 방지
        GameObject label = new GameObject("OrderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 행동 순번 글자 생성
        label.transform.SetParent(root.transform, false); // 순번 배경 안에 글자 연결
        RectTransform labelArea = label.transform as RectTransform; // 순번 글자 영역 조회
        labelArea.anchorMin = Vector2.zero; // 글자 왼쪽 아래 앵커 설정
        labelArea.anchorMax = Vector2.one; // 글자 오른쪽 위 앵커 설정
        labelArea.offsetMin = Vector2.zero; // 글자 왼쪽 아래 여백 제거
        labelArea.offsetMax = Vector2.zero; // 글자 오른쪽 위 여백 제거
        TMP_Text text = label.GetComponent<TMP_Text>(); // 실제 순번 글자 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 기존 전투 한글 글꼴 적용
        text.fontSize = 23f; // 순번 글자 크기 설정
        text.fontStyle = FontStyles.Bold; // 행동 순번 강조 설정
        text.alignment = TextAlignmentOptions.Center; // 행동 순번 중앙 정렬
        text.color = new Color(1f, 0.88f, 0.59f, 1f); // 전투 UI와 어울리는 밝은 금색
        text.raycastTarget = false; // 숫자의 유닛 입력 차단 방지
        BattleEnemyActionOrderView view = root.GetComponent<BattleEnemyActionOrderView>(); // 생성된 순번 화면 조회
        view.orderText = text; // 순번 글자 연결
        return view; // 생성한 순번 화면 반환
    } // 행동 순번 화면 생성 종료
    public void Bind(IReadOnlyList<BattleEnemyAction> actions, BattleUnitRuntime actor) // 실제 적의 남은 행동 순번 표시
    { // 행동 순번 표시 시작
        builder.Clear(); // 이전 행동 순번 문자열 제거
        foreach (BattleEnemyAction action in actions) // 실제 예정 행동 순회
        { // 행동 순번 확인 시작
            if (action == null || action.Actor != actor || action.ActionOrder <= 0) // 현재 적의 유효한 행동 확인
            { // 다른 행동 처리 시작
                continue; // 현재 적과 무관한 행동 제외
            } // 다른 행동 처리 종료
            if (builder.Length > 0) // 같은 적의 이전 행동 확인
            { // 여러 행동 구분 시작
                builder.Append(" · "); // 여러 행동 순번 사이 구분자 추가
            } // 여러 행동 구분 종료
            builder.Append(action.ActionOrder); // 실제 행동 순번 추가
        } // 행동 순번 확인 종료
        string current = builder.ToString(); // 실제 행동 순번 표시 문구 생성
        orderText.text = current; // 적 머리 위에 행동 순번 표시
        gameObject.SetActive(current.Length > 0); // 행동이 없는 적의 순번 숨김
    } // 행동 순번 표시 종료
} // 행동 순번 화면 종료
