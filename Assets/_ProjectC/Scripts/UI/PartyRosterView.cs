using System; // 인수 예외 사용
using System.Collections.Generic; // 화면 항목 목록 사용
using TMPro; // 한글 화면 문자 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 배치 기능 사용
public sealed class PartyRosterView : MonoBehaviour // 거점 캐릭터 편성 화면
{ // 화면 클래스 시작
    private readonly List<CharacterData> characters = new List<CharacterData>(); // 표시 캐릭터 목록
    private readonly List<Button> buttons = new List<Button>(); // 캐릭터 선택 버튼 목록
    private readonly List<TMP_Text> labels = new List<TMP_Text>(); // 캐릭터 표시 문구 목록
    private IPartyDeploymentStatusProvider statusProvider; // 출전 상태 제공자
    private CharacterUnlockProgress unlockProgress; // 캐릭터 해금 상태
    private TMP_Text summary; // 현재 편성 안내 문구
    private Button startButton; // 탐사 출발 버튼
    private CharacterRecoveryManager recoveryManager; // 회복 상태 변경 감시자
    private BattleResultManager resultManager; // 전투 결과 변경 감시자
    public PartySelection Selection { get; private set; } // 현재 편성 상태 조회
    public void Initialize(CharacterRosterData roster, IPartyDeploymentStatusProvider provider, CharacterUnlockProgress progress = null) // 캐릭터 목록 화면 초기화
    { // 초기화 시작
        if (roster == null || provider == null) // 필수 데이터 확인
        { // 데이터 누락 처리 시작
            throw new ArgumentNullException(roster == null ? nameof(roster) : nameof(provider)); // 누락 데이터 예외
        } // 데이터 누락 처리 종료
        if (Selection != null) // 중복 초기화 확인
        { // 중복 처리 시작
            throw new InvalidOperationException("파티 화면이 이미 초기화되었습니다."); // 중복 초기화 예외
        } // 중복 처리 종료
        statusProvider = provider; // 출전 상태 제공자 저장
        unlockProgress = progress ?? CharacterUnlockProgress.Shared; // 해금 상태 저장
        Selection = new PartySelection(provider, unlockProgress); // 빈 파티 선택 상태 생성
        recoveryManager = CharacterRecoveryManager.Instance; // 현재 회복 관리자 조회
        resultManager = BattleResultManager.Instance; // 현재 전투 결과 관리자 조회
        if (recoveryManager != null) // 회복 관리자 존재 확인
        { // 회복 이벤트 연결 시작
            recoveryManager.RecoveryStateChanged += Refresh; // 회복 상태 변경 감시
        } // 회복 이벤트 연결 종료
        if (resultManager != null) // 결과 관리자 존재 확인
        { // 파티 이벤트 연결 시작
            resultManager.PartyStateChanged += Refresh; // 파티 상태 변경 감시
        } // 파티 이벤트 연결 종료
        RectTransform panel = CreateRect("PartyRosterPanel", transform); // 편성 화면 영역 생성
        panel.sizeDelta = new Vector2(840f, 650f); // 화면 영역 크기 설정
        Image background = panel.gameObject.AddComponent<Image>(); // 배경 이미지 추가
        background.color = new Color(0.08f, 0.10f, 0.16f, 0.94f); // 배경 색상 설정
        TMP_Text title = CreateLabel("Title", panel, "파티 편성", 30f); // 화면 제목 생성
        title.rectTransform.anchoredPosition = new Vector2(0f, 290f); // 제목 위치 설정
        title.rectTransform.sizeDelta = new Vector2(780f, 48f); // 제목 크기 설정
        RectTransform grid = CreateRect("CharacterGrid", panel); // 캐릭터 목록 영역 생성
        grid.sizeDelta = new Vector2(780f, 500f); // 목록 영역 크기 설정
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>(); // 격자 배치 추가
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 설정
        layout.constraintCount = 4; // 네 열 배치 설정
        layout.cellSize = new Vector2(185f, 70f); // 버튼 크기 설정
        layout.spacing = new Vector2(10f, 10f); // 버튼 간격 설정
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 캐릭터 버튼 생성 시작
            if (character == null) // 누락 캐릭터 확인
            { // 누락 처리 시작
                continue; // 누락 캐릭터 제외
            } // 누락 처리 종료
            AddCharacterButton(grid, character); // 개별 선택 버튼 추가
        } // 캐릭터 버튼 생성 종료
        summary = CreateLabel("Summary", panel, string.Empty, 20f); // 선택 안내 생성
        summary.rectTransform.anchoredPosition = new Vector2(-120f, -290f); // 안내 위치 설정
        summary.rectTransform.sizeDelta = new Vector2(480f, 48f); // 안내 크기 설정
        RectTransform startRect = CreateRect("StartExploration", panel); // 탐사 출발 버튼 영역 생성
        startRect.anchoredPosition = new Vector2(285f, -290f); // 출발 버튼 위치 설정
        startRect.sizeDelta = new Vector2(180f, 48f); // 출발 버튼 크기 설정
        Image startImage = startRect.gameObject.AddComponent<Image>(); // 출발 버튼 배경 생성
        startImage.color = new Color(0.23f, 0.38f, 0.42f); // 출발 버튼 색상 설정
        startButton = startRect.gameObject.AddComponent<Button>(); // 출발 버튼 컴포넌트 생성
        startButton.targetGraphic = startImage; // 출발 버튼 배경 연결
        TMP_Text startLabel = CreateLabel("Label", startRect, "탐사 출발", 22f); // 출발 버튼 문구 생성
        startLabel.rectTransform.anchorMin = Vector2.zero; // 문구 시작점 설정
        startLabel.rectTransform.anchorMax = Vector2.one; // 문구 끝점 설정
        startLabel.rectTransform.offsetMin = Vector2.zero; // 문구 시작 여백 제거
        startLabel.rectTransform.offsetMax = Vector2.zero; // 문구 끝 여백 제거
        startButton.onClick.AddListener(StartExploration); // 탐사 출발 동작 연결
        Refresh(); // 최초 상태 표시
    } // 초기화 종료
    public void Refresh() // 캐릭터 선택 상태 갱신
    { // 상태 갱신 시작
        if (Selection == null || summary == null || startButton == null) // 화면 초기화 상태 확인
        { // 초기화 전 처리 시작
            return; // 상태 갱신 중단
        } // 초기화 전 처리 종료
        for (int index = 0; index < characters.Count; index++) // 표시 캐릭터 순회
        { // 버튼 갱신 시작
            CharacterData character = characters[index]; // 현재 캐릭터 조회
            bool selected = ContainsMember(character); // 현재 선택 여부 확인
            bool lockedWorld = !unlockProgress.IsUnlocked(character); // 캐릭터 해금 상태 확인
            int remaining = recoveryManager != null ? recoveryManager.GetRemainingRecoveryExpeditions(character) : 0; // 남은 회복 탐사 횟수 조회
            PartyMemberDeploymentViewState state = PartyDeploymentViewStateFactory.Create(character, statusProvider, remaining); // 출전 표시 상태 계산
            buttons[index].interactable = !lockedWorld && (selected || state.CanSelect); // 선택 가능 상태 설정
            buttons[index].image.color = selected ? new Color(0.28f, 0.48f, 0.35f) : new Color(0.22f, 0.25f, 0.34f); // 선택 배경 색상 설정
            labels[index].text = $"{character.DisplayName}\n{(lockedWorld ? "10층 보스 승리 시 해금" : selected ? "선택됨" : state.StatusText)}"; // 이름과 상태 표시
        } // 버튼 갱신 종료
        summary.text = $"선택 {Selection.Members.Count}/4 · 임시 시작 카드 사용"; // 현재 선택 인원과 임시 덱 안내
        startButton.interactable = Selection.Validate().CanDeploy; // 출전 가능한 파티만 출발 허용
    } // 상태 갱신 종료
    private void AddCharacterButton(Transform parent, CharacterData character) // 캐릭터 선택 버튼 생성
    { // 버튼 생성 시작
        RectTransform rect = CreateRect(character.CharacterId, parent); // 버튼 영역 생성
        Image image = rect.gameObject.AddComponent<Image>(); // 버튼 이미지 추가
        Button button = rect.gameObject.AddComponent<Button>(); // 선택 버튼 추가
        button.targetGraphic = image; // 버튼 배경 연결
        RectTransform portraitRect = CreateRect("Portrait", rect); // 초상화 영역 생성
        portraitRect.anchorMin = new Vector2(0f, 0.5f); // 초상화 기준 시작점 설정
        portraitRect.anchorMax = new Vector2(0f, 0.5f); // 초상화 기준 끝점 설정
        portraitRect.anchoredPosition = new Vector2(32f, 0f); // 초상화 위치 설정
        portraitRect.sizeDelta = new Vector2(52f, 52f); // 초상화 크기 설정
        Image portrait = portraitRect.gameObject.AddComponent<Image>(); // 초상화 이미지 추가
        portrait.sprite = character.Portrait; // 캐릭터 초상화 연결
        portrait.preserveAspect = true; // 초상화 비율 유지
        portrait.raycastTarget = false; // 버튼 입력 방해 방지
        portrait.enabled = character.Portrait != null; // 초상화가 있을 때만 표시
        TMP_Text label = CreateLabel("Label", rect, character.DisplayName, 20f); // 캐릭터 문구 생성
        label.enableAutoSizing = true; // 긴 상태 문구 크기 자동 조정
        label.fontSizeMin = 12f; // 최소 글자 크기 설정
        label.fontSizeMax = 20f; // 최대 글자 크기 설정
        label.rectTransform.anchorMin = Vector2.zero; // 문구 시작점 설정
        label.rectTransform.anchorMax = Vector2.one; // 문구 끝점 설정
        label.rectTransform.offsetMin = new Vector2(60f, 0f); // 초상화와 문구 간격 설정
        label.rectTransform.offsetMax = Vector2.zero; // 문구 오른쪽 위 여백 제거
        button.onClick.AddListener(() => ToggleCharacter(character)); // 버튼 선택 동작 연결
        characters.Add(character); // 표시 캐릭터 등록
        buttons.Add(button); // 선택 버튼 등록
        labels.Add(label); // 표시 문구 등록
    } // 버튼 생성 종료
    private void ToggleCharacter(CharacterData character) // 캐릭터 선택 상태 전환
    { // 선택 전환 시작
        if (ContainsMember(character)) // 기존 선택 여부 확인
        { // 선택 해제 시작
            Selection.TryRemove(character); // 캐릭터 선택 해제
        } // 선택 해제 종료
        else // 선택되지 않은 캐릭터 확인
        { // 선택 추가 시작
            Selection.TryAdd(character); // 출전 가능 캐릭터 선택
        } // 선택 추가 종료
        Refresh(); // 변경된 선택 상태 표시
    } // 선택 전환 종료
    private void StartExploration() // 선택한 파티로 탐사 출발
    { // 출발 처리 시작
        if (SceneFlowManager.Instance == null || SceneFlowManager.Instance.IsLoadingScene || !Application.CanStreamedLevelBeLoaded("30_Exploration")) // 씬 전환 가능 상태 확인
        { // 씬 전환 불가 처리 시작
            summary.text = "탐사 화면을 준비할 수 없습니다."; // 씬 전환 실패 안내
            return; // 탐사 출발 중단
        } // 씬 전환 불가 처리 종료
        if (!PartyLoadoutRuntime.Prepare(Selection)) // 파티와 임시 덱 준비 확인
        { // 편성 실패 처리 시작
            Refresh(); // 현재 선택 상태 재검증
            summary.text = "출전 가능 파티와 시작 카드를 확인하세요."; // 편성 실패 안내
            return; // 탐사 출발 중단
        } // 편성 실패 처리 종료
        ExplorationSessionManager.EnsureInstance().ResetExploration(); // 새 탐사 상태 초기화
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 선택한 시작 덱 적용
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 탐사 화면 이동
    } // 출발 처리 종료
    private bool ContainsMember(CharacterData character) // 선택 목록 포함 여부 조회
    { // 포함 확인 시작
        foreach (CharacterData member in Selection.Members) // 선택 캐릭터 순회
        { // 캐릭터 비교 시작
            if (member == character) // 동일 캐릭터 확인
            { // 일치 처리 시작
                return true; // 선택 상태 반환
            } // 일치 처리 종료
        } // 캐릭터 비교 종료
        return false; // 미선택 상태 반환
    } // 포함 확인 종료
    private static RectTransform CreateRect(string objectName, Transform parent) // 화면 영역 생성
    { // 영역 생성 시작
        GameObject item = new GameObject(objectName, typeof(RectTransform)); // 화면 객체 생성
        RectTransform rect = item.GetComponent<RectTransform>(); // 영역 컴포넌트 조회
        rect.SetParent(parent, false); // 부모 화면 연결
        return rect; // 생성 영역 반환
    } // 영역 생성 종료
    private static TMP_Text CreateLabel(string objectName, Transform parent, string value, float fontSize) // 화면 문자 생성
    { // 문자 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 문자 영역 생성
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); // 문자 컴포넌트 추가
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 폰트 적용
        text.text = value; // 표시 문구 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 가운데 정렬 설정
        text.raycastTarget = false; // 버튼 입력 차단 방지
        return text; // 생성 문자 반환
    } // 문자 생성 종료
    private void OnDestroy() // 편성 화면 제거 처리
    { // 화면 정리 시작
        if (recoveryManager != null) // 회복 관리자 존재 확인
        { // 회복 이벤트 해제 시작
            recoveryManager.RecoveryStateChanged -= Refresh; // 회복 상태 감시 해제
        } // 회복 이벤트 해제 종료
        if (resultManager != null) // 결과 관리자 존재 확인
        { // 파티 이벤트 해제 시작
            resultManager.PartyStateChanged -= Refresh; // 파티 상태 감시 해제
        } // 파티 이벤트 해제 종료
    } // 화면 정리 종료
} // 화면 클래스 종료
