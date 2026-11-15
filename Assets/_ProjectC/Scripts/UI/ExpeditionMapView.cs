using TMPro; // 맵 선택 문구 사용
using UnityEngine; // 탐사 화면 객체 사용
using UnityEngine.UI; // 맵 선택 버튼 사용

public sealed class ExpeditionMapView : MonoBehaviour // 탐사 맵 선택 화면
{ // 맵 선택 화면 시작
    private Canvas canvas; // 다음 화면 표시 캔버스
    private Button confirmButton; // 선택 맵 확인 버튼
    private TMP_Text selectionText; // 선택 맵 상태 문구
    private bool selected; // 기본 탐사 맵 선택 상태
    public static ExpeditionMapView Open(Canvas targetCanvas) // 맵 선택 화면 열기
    { // 맵 선택 화면 열기 시작
        if (targetCanvas == null || FindFirstObjectByType<ExpeditionMapView>() != null || FindFirstObjectByType<PartyFormationView>() != null) // 캔버스와 중복 화면 확인
        { // 화면 열기 불가 처리 시작
            return null; // 맵 선택 화면 생성 중단
        } // 화면 열기 불가 처리 종료
        RectTransform root = PreparationUi.CreateOverlay(targetCanvas, "MapSelection"); // 맵 선택 전체 화면 생성
        ExpeditionMapView view = root.gameObject.AddComponent<ExpeditionMapView>(); // 맵 선택 동작 추가
        view.canvas = targetCanvas; // 부모 캔버스 보관
        view.Build(); // 맵 선택 화면 구성
        return view; // 생성 화면 반환
    } // 맵 선택 화면 열기 종료
    private void Build() // 맵 선택 화면 요소 배치
    { // 맵 선택 구성 시작
        TMP_Text title = PreparationUi.CreateText("Title", transform, "탐사 지역 선택", 44f); // 맵 선택 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900f, 90f)); // 제목 위치 설정
        PreparationUi.CreateButton("BackButton", transform, "뒤로", new Vector2(0.10f, 0.92f), Vector2.zero, new Vector2(220f, 70f), Close); // 로비 복귀 버튼 생성
        PreparationUi.CreateButton("MapOption_0", transform, "기본 탐사", new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(650f, 230f), SelectMap); // 현재 구현된 탐사 맵 선택 버튼 생성
        selectionText = PreparationUi.CreateText("SelectionStatus", transform, "지역을 선택하세요", 25f); // 맵 선택 상태 문구 생성
        PreparationUi.Place(selectionText.rectTransform, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(900f, 70f)); // 선택 상태 위치 설정
        confirmButton = PreparationUi.CreateButton("ConfirmButton", transform, "편성하기", new Vector2(0.84f, 0.09f), Vector2.zero, new Vector2(280f, 80f), Confirm); // 편성 진행 버튼 생성
        confirmButton.interactable = false; // 맵 선택 전 진행 차단
    } // 맵 선택 구성 종료
    private void SelectMap() // 기본 탐사 맵 선택
    { // 맵 선택 시작
        selected = true; // 맵 선택 상태 기록
        selectionText.text = "기본 탐사를 선택했습니다."; // 선택 상태 안내
        confirmButton.interactable = true; // 맵 선택 후 편성 허용
    } // 맵 선택 종료
    private void Confirm() // 선택한 맵에서 편성 화면 열기
    { // 맵 선택 확인 시작
        if (!selected) // 맵 선택 여부 확인
        { // 선택 없음 처리 시작
            return; // 편성 화면 열기 중단
        } // 선택 없음 처리 종료
        gameObject.SetActive(false); // 맵 선택 화면 입력 차단
        if (PartyFormationView.Open(canvas) == null) // 새 편성 화면 생성 확인
        { // 편성 화면 생성 실패 처리 시작
            gameObject.SetActive(true); // 맵 선택 화면 다시 표시
            selectionText.text = "편성 화면을 열 수 없습니다."; // 편성 화면 실패 안내
            return; // 맵 선택 화면 유지
        } // 편성 화면 생성 실패 처리 종료
        Destroy(gameObject); // 이전 맵 화면 제거
    } // 맵 선택 확인 종료
    private void Close() // 맵 선택 화면 닫기
    { // 맵 선택 닫기 시작
        gameObject.SetActive(false); // 맵 선택 화면 입력 차단
        Destroy(gameObject); // 맵 선택 화면 제거
    } // 맵 선택 닫기 종료
} // 맵 선택 화면 종료
