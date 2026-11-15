using TMPro; // 캐릭터 이름 표시 사용
using UnityEngine; // 캐릭터 화면 객체 사용
using UnityEngine.UI; // 목록과 이미지 사용

public sealed class CharacterListView : MonoBehaviour // 로비 캐릭터 목록 화면
{ // 캐릭터 목록 화면 시작
    public static CharacterListView Open(Canvas canvas) // 캐릭터 목록 열기
    { // 목록 열기 시작
        if (canvas == null || FindFirstObjectByType<CharacterListView>() != null) // 캔버스와 중복 화면 확인
        { // 화면 열기 불가 처리 시작
            return null; // 중복 화면 생성 중단
        } // 화면 열기 불가 처리 종료
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        if (roster == null) // 캐릭터 원본 존재 확인
        { // 캐릭터 원본 누락 처리 시작
            Debug.LogWarning("[CharacterList] 캐릭터 목록을 찾을 수 없습니다."); // 원본 누락 안내
            return null; // 목록 생성 중단
        } // 캐릭터 원본 누락 처리 종료
        RectTransform root = PreparationUi.CreateOverlay(canvas, "CharacterList"); // 전체 화면 목록 배경 생성
        CharacterListView view = root.gameObject.AddComponent<CharacterListView>(); // 캐릭터 목록 동작 연결
        view.Build(roster); // 제목과 캐릭터 목록 구성
        return view; // 생성 화면 반환
    } // 목록 열기 종료
    private void Build(CharacterRosterData roster) // 캐릭터 목록 배치
    { // 목록 배치 시작
        TMP_Text title = PreparationUi.CreateText("Title", transform, "캐릭터", 42f); // 캐릭터 화면 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(600f, 70f)); // 제목 위치 설정
        PreparationUi.CreateButton("BackButton", transform, "뒤로", new Vector2(0.085f, 0.93f), Vector2.zero, new Vector2(200f, 70f), Close); // 로비 복귀 버튼 생성
        RectTransform panel = PreparationUi.CreatePanel("ListPanel", transform, new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.86f)); // 캐릭터 목록 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 기존 화면 패널 디자인 적용
        ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>(); // 캐릭터 목록 스크롤 추가
        RectTransform viewport = PreparationUi.CreatePanel("Viewport", panel, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f)); // 목록 표시 영역 생성
        viewport.GetComponent<Image>().color = new Color(0.02f, 0.045f, 0.05f, 0.88f); // 목록 안쪽 배경 색상 설정
        viewport.gameObject.AddComponent<RectMask2D>(); // 목록 영역 밖 캐릭터 가리기
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup)); // 캐릭터 격자 객체 생성
        gridObject.transform.SetParent(viewport, false); // 표시 영역에 격자 연결
        RectTransform grid = gridObject.GetComponent<RectTransform>(); // 격자 영역 조회
        grid.anchorMin = new Vector2(0f, 1f); // 격자 시작 앵커 설정
        grid.anchorMax = Vector2.one; // 격자 끝 앵커 설정
        grid.pivot = new Vector2(0.5f, 1f); // 격자 위쪽 기준 설정
        int rows = (roster.Characters.Count + 5) / 6; // 여섯 열 기준 행 수 계산
        grid.sizeDelta = new Vector2(0f, 40f + rows * 300f + Mathf.Max(0, rows - 1) * 24f); // 전체 목록 높이 설정
        grid.anchoredPosition = Vector2.zero; // 격자 상단 위치 설정
        GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>(); // 캐릭터 격자 배치 조회
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 설정
        layout.constraintCount = 6; // 캐릭터 여섯 열 배치
        layout.cellSize = new Vector2(250f, 300f); // 캐릭터 카드 크기 설정
        layout.spacing = new Vector2(24f, 24f); // 캐릭터 카드 간격 설정
        layout.padding = new RectOffset(20, 20, 20, 20); // 캐릭터 격자 여백 설정
        layout.childAlignment = TextAnchor.UpperCenter; // 캐릭터 격자 가운데 정렬
        scroll.viewport = viewport; // 스크롤 표시 영역 연결
        scroll.content = grid; // 스크롤 내용 영역 연결
        scroll.horizontal = false; // 가로 스크롤 차단
        scroll.vertical = true; // 세로 스크롤 허용
        scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 범위 밖 이동 차단
        scroll.scrollSensitivity = 36f; // 마우스 휠 이동 폭 설정
        for (int index = 0; index < roster.Characters.Count; index++) // 실제 캐릭터 목록 순회
        { // 캐릭터 카드 생성 시작
            CreateCard(grid, roster.Characters[index], index); // 캐릭터별 이미지와 이름 표시
        } // 캐릭터 카드 생성 종료
    } // 목록 배치 종료
    private void CreateCard(Transform parent, CharacterData character, int index) // 캐릭터 카드 생성
    { // 캐릭터 카드 생성 시작
        RectTransform card = PreparationUi.CreatePanel("Character_" + index, parent, Vector2.zero, Vector2.one); // 캐릭터 카드 배경 생성
        LobbyUiArt.Apply(card.GetComponent<Image>(), "FormationCard"); // 캐릭터 카드 디자인 적용
        RectTransform portraitRect = PreparationUi.CreatePanel("Portrait", card, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.94f)); // 캐릭터 초상화 영역 생성
        Image portrait = portraitRect.GetComponent<Image>(); // 초상화 이미지 조회
        portrait.sprite = character != null ? character.Portrait : null; // 실제 캐릭터 초상화 적용
        portrait.preserveAspect = true; // 초상화 원본 비율 유지
        bool unlocked = character != null && CharacterUnlockProgress.Shared.IsUnlocked(character); // 캐릭터 해금 상태 확인
        portrait.color = unlocked ? Color.white : new Color(0.36f, 0.36f, 0.36f, 1f); // 해금 상태별 초상화 밝기 설정
        TMP_Text name = PreparationUi.CreateText("Name", card, character != null ? character.DisplayName : "알 수 없음", 26f); // 실제 캐릭터 이름 생성
        PreparationUi.Stretch(name.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.2f)); // 이름 표시 영역 설정
        if (!unlocked) // 해금되지 않은 캐릭터 확인
        { // 잠금 상태 표시 시작
            TMP_Text locked = PreparationUi.CreateText("Locked", card, "잠김", 24f); // 해금 전 상태 문구 생성
            PreparationUi.Place(locked.rectTransform, new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(180f, 45f)); // 잠금 문구 위치 설정
        } // 잠금 상태 표시 종료
    } // 캐릭터 카드 생성 종료
    private void Close() // 캐릭터 화면 닫기
    { // 화면 닫기 시작
        gameObject.SetActive(false); // 닫는 화면 입력 차단
        Destroy(gameObject); // 캐릭터 목록 화면 제거
    } // 화면 닫기 종료
} // 캐릭터 목록 화면 종료
