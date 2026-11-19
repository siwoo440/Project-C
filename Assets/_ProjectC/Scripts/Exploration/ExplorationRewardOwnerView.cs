using System; // 보상 선택 콜백 사용
using TMPro; // 한글 안내 문구 사용
using UnityEngine; // 보상 화면 객체 사용
using UnityEngine.EventSystems; // 화면 입력 시스템 사용
using UnityEngine.InputSystem.UI; // 신규 입력 시스템 사용
using UnityEngine.UI; // 보상 선택 버튼 사용

public sealed class ExplorationRewardOwnerView : MonoBehaviour // 보물 카드 소유자 선택 화면
{ // 선택 화면 시작
    private static ExplorationRewardOwnerView active; // 현재 열린 선택 화면
    private float previousTimeScale; // 선택 전 탐사 시간 배율
    private bool paused; // 선택 화면 시간 정지 여부
    private Func<CharacterData, bool> onChosen; // 소유자 선택 처리
    private Action<CharacterData> afterChosen; // 선택 화면 종료 후 다음 선택 처리

    public static bool Open(Transform parent, ShopOfferData offer, Func<CharacterData, bool> chosen) // 보상 소유자 선택 열기
    { // 선택 화면 열기 시작
        PartyData party = PartyLoadoutRuntime.Active != null ? PartyLoadoutRuntime.Active.Party : null; // 현재 탐사 파티 조회
        return offer != null && OpenPartyMember(parent, party, $"보물 카드: {offer.DisplayName}", "카드를 받을 파티원을 선택하세요.", "나중에 선택", null, chosen); // 보물 카드 소유자 선택 화면 표시
    } // 보물 선택 화면 열기 종료

    public static bool OpenPartyMember(Transform parent, PartyData party, string heading, string instruction, string cancelLabel, Predicate<CharacterData> canChoose, Func<CharacterData, bool> chosen, Action<CharacterData> next = null) // 탐사 선택 대상 표시
    { // 대상 선택 화면 열기 시작
        if (active != null || parent == null || chosen == null || party == null || party.MemberCount == 0) // 선택 화면 필수 조건 확인
        { // 선택 화면 열기 실패 시작
            return false; // 유효하지 않은 선택 화면 차단
        } // 선택 화면 열기 실패 종료
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null) // 입력 시스템 존재 확인
        { // 입력 시스템 생성 시작
            new GameObject("RewardEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 신규 입력 시스템 생성
        } // 입력 시스템 생성 종료
        GameObject canvasObject = new GameObject("TreasureOwnerCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // 선택 화면 캔버스 생성
        canvasObject.transform.SetParent(parent, false); // 보물 방 수명과 연결
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 선택 캔버스 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 위 표시 설정
        Canvas parentCanvas = parent.GetComponentInParent<Canvas>(); // 기존 이벤트 화면 캔버스 조회
        canvas.overrideSorting = parentCanvas != null; // 이벤트 패널 앞에 표시할 순서 독립
        canvas.sortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 1 : 600; // 열린 탐사 화면보다 앞에 표시
        if (parentCanvas != null) // 기존 이벤트 화면 안쪽 배치 확인
        { // 중첩 캔버스 크기 지정 시작
            PreparationUi.Stretch((RectTransform)canvasObject.transform, Vector2.zero, Vector2.one); // 부모 화면 전체에 대상 선택 영역 맞춤
        } // 중첩 캔버스 크기 지정 종료
        else // 독립 보물 화면 배치
        { // 화면 크기 보정 시작
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); // 독립 화면 크기 조절기 추가
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도별 화면 크기 조절
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 지정
        } // 화면 크기 보정 종료
        ExplorationRewardOwnerView view = canvasObject.AddComponent<ExplorationRewardOwnerView>(); // 선택 화면 동작 연결
        active = view; // 현재 선택 화면 저장
        view.onChosen = chosen; // 지급 콜백 저장
        view.afterChosen = next; // 대상 선택 후 다음 화면 처리 연결
        view.previousTimeScale = Time.timeScale; // 기존 시간 배율 저장
        view.paused = true; // 시간 정지 상태 기록
        Time.timeScale = 0f; // 선택 중 탐사 정지
        RectTransform shade = PreparationUi.CreatePanel("Shade", canvasObject.transform, Vector2.zero, Vector2.one); // 전체 화면 어두운 배경 생성
        shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f); // 배경 어둡기 설정
        RectTransform panel = PreparationUi.CreatePanel("Panel", canvasObject.transform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f)); // 중앙 선택 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 기존 패널 이미지 적용
        TMP_Text title = PreparationUi.CreateText("Title", panel, heading, 38f); // 선택 행동 제목 생성
        PreparationUi.Stretch(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.95f)); // 제목 영역 지정
        TMP_Text hint = PreparationUi.CreateText("Hint", panel, instruction, 26f); // 파티원 선택 안내 생성
        PreparationUi.Stretch(hint.rectTransform, new Vector2(0.08f, 0.67f), new Vector2(0.92f, 0.78f)); // 안내 영역 지정
        int count = party.Members.Count; // 파티 슬롯 수 조회
        for (int index = 0; index < count; index++) // 파티 슬롯 순회
        { // 파티원 버튼 생성 시작
            CharacterData member = party.Members[index]; // 현재 파티원 조회
            if (member == null) // 빈 파티 슬롯 확인
            { // 빈 슬롯 처리 시작
                continue; // 다음 파티원 확인
            } // 빈 슬롯 처리 종료
            CharacterData captured = member; // 버튼용 파티원 보관
            float horizontal = (index - (count - 1) * 0.5f) * 250f; // 파티원 가로 위치 계산
            Button button = PreparationUi.CreateButton("Owner_" + member.CharacterId, panel, member.DisplayName, new Vector2(0.5f, 0.5f), new Vector2(horizontal, -25f), new Vector2(220f, 300f), () => view.Choose(captured)); // 파티원 선택 버튼 생성
            button.interactable = canChoose == null || canChoose(member); // 행동 가능한 파티원만 선택 허용
            LobbyUiArt.Apply(button.GetComponent<Image>(), "FormationPanel"); // 파티원 선택 배경 적용
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(); // 파티원 이름 조회
            PreparationUi.Stretch(label.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.22f)); // 이름 하단 배치
            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 파티원 초상화 생성
            portraitObject.transform.SetParent(button.transform, false); // 선택 버튼에 초상화 연결
            Image portrait = portraitObject.GetComponent<Image>(); // 초상화 이미지 조회
            portrait.sprite = member.Portrait; // 파티원 프로필 적용
            portrait.preserveAspect = true; // 초상화 원본 비율 유지
            portrait.raycastTarget = false; // 초상화의 버튼 입력 방해 방지
            PreparationUi.Stretch(portrait.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.93f)); // 초상화 영역 지정
        } // 파티원 버튼 생성 종료
        PreparationUi.CreateButton("CancelButton", panel, cancelLabel, new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(240f, 64f), view.Close); // 비용 없는 선택 취소 버튼 생성
        return true; // 선택 화면 표시 성공 반환
    } // 선택 화면 열기 종료

    public static void ClosePending() // 열려 있는 보상 선택 종료
    { // 보상 선택 종료 시작
        if (active != null) // 열린 선택 화면 확인
        { // 열린 화면 닫기 시작
            active.Close(); // 시간 복원과 화면 제거
        } // 열린 화면 닫기 종료
    } // 보상 선택 종료 완료

    private void Choose(CharacterData owner) // 선택한 파티원 보상 처리
    { // 파티원 선택 시작
        if (active == this && onChosen != null && onChosen(owner)) // 현재 선택 화면의 유효한 확정 확인
        { // 보상 선택 완료 시작
            Close(); // 지급 완료 후 선택 화면 닫기
            afterChosen?.Invoke(owner); // 비용 없는 중간 선택 후 다음 화면 열기
        } // 보상 선택 완료 종료
    } // 파티원 선택 종료

    private void Close() // 선택 화면 닫기
    { // 화면 닫기 시작
        if (active == this) // 현재 화면 여부 확인
        { // 현재 화면 정리 시작
            active = null; // 현재 선택 화면 해제
        } // 현재 화면 정리 종료
        if (paused) // 선택 화면 시간 정지 확인
        { // 탐사 시간 복원 시작
            Time.timeScale = previousTimeScale; // 이전 시간 배율 복원
            paused = false; // 시간 정지 상태 해제
        } // 탐사 시간 복원 종료
        gameObject.SetActive(false); // 종료된 선택 화면 즉시 숨김
        transform.SetParent(null, false); // 재선택 시 이전 창과 구분
        Destroy(gameObject); // 선택 화면 객체 제거
    } // 화면 닫기 종료

    private void OnDestroy() // 화면 파괴 시 정리
    { // 화면 파괴 정리 시작
        if (active == this) // 현재 화면 여부 확인
        { // 현재 화면 해제 시작
            active = null; // 파괴된 화면 참조 해제
        } // 현재 화면 해제 종료
        if (paused) // 파괴 전 시간 정지 확인
        { // 탐사 시간 복원 시작
            Time.timeScale = previousTimeScale; // 이전 시간 배율 복원
            paused = false; // 시간 정지 상태 해제
        } // 탐사 시간 복원 종료
    } // 화면 파괴 정리 종료
} // 선택 화면 종료
