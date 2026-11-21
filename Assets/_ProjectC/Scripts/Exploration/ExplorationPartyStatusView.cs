using System.Collections.Generic; // 파티 카드 UI 목록 사용
using TMPro; // 한글 TMP 텍스트 사용
using UnityEngine; // 런타임 UI와 색상 사용
using UnityEngine.UI; // Canvas와 Image UI 사용

public sealed class ExplorationPartyStatusView : MonoBehaviour // 탐사 좌하단 출전 파티 상태 HUD
{
    private const float RefreshInterval = 0.2f; // 상태 폴링 보조 갱신 간격
    private const float CardWidth = 206f; // 초상화와 두 게이지를 담을 프로필 폭
    private const float CardHeight = 100f; // 좌하단 프로필 전체 높이
    private const float CardSpacing = 10f; // 파티 프로필 사이 간격
    private static readonly Color HealthFillColor = new Color(0.64f, 0.18f, 0.25f, 1f); // 체력 게이지의 짙은 붉은색
    private static readonly Color MentalFillColor = new Color(0.20f, 0.42f, 0.69f, 1f); // 정신력 게이지의 짙은 푸른색
    private static readonly Color FallenFillColor = new Color(0.30f, 0.31f, 0.34f, 1f); // 사망 상태의 차분한 게이지 색상
    private static readonly Vector2 PortraitPosition = new Vector2(10f, 12f); // 초상화와 사망 표시의 안쪽 위치
    private static readonly Vector2 PortraitSize = new Vector2(68f, 76f); // 초상화와 사망 표시의 공통 크기

    private sealed class VitalView // 체력 또는 정신력 한 줄의 표시 참조
    { // 게이지 참조 시작
        public Image Fill; // 잔량에 따라 가로 길이가 바뀌는 채움 이미지
        public TMP_Text Value; // 현재값과 최대값 글자

        public void Refresh(int current, int maximum, Color fillColor, Color textColor) // 실제 값과 상태 색상 갱신
        { // 게이지 갱신 시작
            float fraction = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f; // 최대값에 대한 안전한 채움 비율
            Fill.rectTransform.anchorMax = new Vector2(fraction, 1f); // 트랙 왼쪽부터 남은 양만큼 채움
            Fill.color = fillColor; // 생존 상태에 맞는 채움 색상
            Value.text = $"{current}/{maximum}"; // 현재값과 최대값 표기
            Value.color = textColor; // 생존 상태에 맞는 수치 색상
        } // 게이지 갱신 종료
    } // 게이지 참조 종료

    private sealed class MemberView // 캐릭터 한 명의 HUD 참조 모음
    {
        public CharacterData Character; // 표시 캐릭터 데이터
        public Image Portrait; // 캐릭터 초상화
        public TMP_Text NameText; // 캐릭터 이름
        public VitalView Health; // 상태 관리자에 연결된 체력 게이지
        public VitalView Mental; // 상태 관리자에 연결된 정신력 게이지
        public Image DeathShade; // 사망 흐림 오버레이
        public TMP_Text DeathText; // 빨간 사망 문구
    }

    private readonly List<MemberView> memberViews =
        new List<MemberView>(); // 현재 출전 파티 HUD 목록

    private BattleResultManager resultManager; // 파티 영구 상태 관리자
    private GameObject canvasObject; // 런타임 파티 HUD Canvas
    private RectTransform partyRoot; // 좌하단 파티 카드 부모
    private PartyData boundParty; // 현재 표시 중인 파티
    private float nextRefreshTime; // 다음 보조 갱신 시각

    internal void ShowMapFragmentNotice(string message) // 지도 조각 접촉 결과를 탐사 화면에 잠시 표시
    { // 조각 알림 표시 시작
        EnsureUi(); // 탐사 상태 캔버스 확보
        var panel = PreparationUi.CreatePanel("MapFragmentNotice", canvasObject.transform, Vector2.zero, Vector2.one); // 전투 회피 안내판 생성
        PreparationUi.Place(panel, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(670f, 62f)); // HUD와 겹치지 않는 상단 중앙 배치
        panel.GetComponent<Image>().raycastTarget = false; // 알림 중 탐사 입력 허용
        TMP_Text label = PreparationUi.CreateText("Result", panel, message, 26f); // 한글 결과 문구 표시
        PreparationUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one); // 안내판 안에 문구 배치
        Destroy(panel.gameObject, 3f); // 세 초 뒤 탐사 안내 제거
    } // 조각 알림 표시 종료

    public void Configure(
        BattleResultManager targetResultManager,
        ExplorationSessionManager targetSessionManager) // 파티 HUD 관리자 연결
    {
        if (resultManager != null)
        {
            resultManager.PartyStateChanged -= HandlePartyStateChanged; // 이전 상태 변경 이벤트 해제
        }

        resultManager =
            targetResultManager; // 파티 상태 관리자 저장

        if (resultManager != null)
        {
            resultManager.PartyStateChanged += HandlePartyStateChanged; // 파티 상태 변경 즉시 갱신 등록
        }

        EnsureUi(); // 런타임 좌하단 Canvas 준비
        RebuildIfNeeded(); // 현재 파티 기준 HUD 생성
        RefreshAll(); // 최초 상태 표시
    }

    private void Update() // 파티 상태 보조 갱신
    {
        if (resultManager == null)
        {
            return;
        }

        RebuildIfNeeded(); // 파티 교체 여부 확인

        if (Time.unscaledTime >=
            nextRefreshTime)
        {
            nextRefreshTime =
                Time.unscaledTime +
                RefreshInterval; // 다음 보조 갱신 시각 설정

            RefreshAll(); // 현재 HP·정신력·사망 표시 갱신
        }

    }

    private void OnDestroy() // 파티 HUD 제거 처리
    {
        if (resultManager != null)
        {
            resultManager.PartyStateChanged -= HandlePartyStateChanged; // 상태 변경 이벤트 해제
        }
    }

    private void HandlePartyStateChanged() // 파티 영구 상태 변경 처리
    {
        RebuildIfNeeded(); // 파티 변경 시 카드 재구성
        RefreshAll(); // HP·정신력·사망 표시 즉시 갱신
    }

    private void EnsureUi() // 탐사 좌하단 파티 HUD Canvas 생성
    {
        if (canvasObject != null)
        {
            return;
        }

        canvasObject =
            new GameObject(
                "ExplorationPartyStatusCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)); // 파티 상태 Canvas 생성

        canvasObject.transform.SetParent(
            transform,
            false); // 탐사 런타임 하위 배치

        Canvas canvas =
            canvasObject.GetComponent<Canvas>(); // 생성 Canvas 조회

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay; // 화면 고정 HUD 방식 설정

        canvas.sortingOrder =
            1600; // 탐사 일반 HUD보다 위 표시

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>(); // Canvas 해상도 보정기 조회

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 비율 사용

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f); // 프로젝트 기준 해상도 설정

        scaler.matchWidthOrHeight =
            0.5f; // 가로·세로 중간 비율 보정

        GameObject rootObject =
            new GameObject(
                "PartyRoot",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup)); // 좌하단 파티 카드 부모 생성

        rootObject.transform.SetParent(
            canvasObject.transform,
            false); // Canvas 하위 배치

        partyRoot =
            rootObject.GetComponent<RectTransform>(); // 파티 부모 RectTransform 조회

        partyRoot.anchorMin =
            Vector2.zero; // 화면 왼쪽 아래 기준 설정

        partyRoot.anchorMax =
            Vector2.zero; // 화면 왼쪽 아래 기준 설정

        partyRoot.pivot =
            Vector2.zero; // 좌하단 Pivot 설정

        partyRoot.anchoredPosition =
            new Vector2(
                20f,
                20f); // 화면 왼쪽 아래 여백 설정

        float partyWidth = CardWidth * PartyData.MaxPartySize + CardSpacing * (PartyData.MaxPartySize - 1); // 네 프로필과 사이 간격의 전체 폭
        partyRoot.sizeDelta = new Vector2(partyWidth, CardHeight); // 한 줄로 네 명을 표시하는 화면 영역

        HorizontalLayoutGroup layout =
            rootObject.GetComponent<HorizontalLayoutGroup>(); // 가로 파티 배치기 조회

        layout.spacing = CardSpacing; // 캐릭터 프로필 사이 간격 적용
        layout.childAlignment = TextAnchor.LowerLeft; // 좌하단 기준 배치
        layout.childControlWidth = false; // 카드 자체 폭 사용
        layout.childControlHeight = false; // 카드 자체 높이 사용
        layout.childForceExpandWidth = false; // 카드 강제 확장 비활성화
        layout.childForceExpandHeight = false; // 카드 강제 확장 비활성화
    }

    private void RebuildIfNeeded() // 현재 출전 파티 변경 시 HUD 재구성
    {
        PartyData activeParty =
            resultManager != null
                ? resultManager.ActiveParty
                : null; // 현재 출전 파티 조회

        if (activeParty == boundParty)
        {
            return;
        }

        boundParty =
            activeParty; // 표시 파티 참조 갱신

        ClearMemberViews(); // 기존 파티 카드 제거

        if (boundParty == null)
        {
            if (partyRoot != null)
            {
                partyRoot.gameObject.SetActive(false); // 파티 미등록 시 HUD 숨김
            }

            return;
        }

        partyRoot.gameObject.SetActive(true); // 출전 파티 HUD 표시

        foreach (CharacterData characterData in boundParty.Members)
        {
            if (characterData == null)
            {
                continue;
            }

            memberViews.Add(
                CreateMemberView(
                    characterData)); // 출전 캐릭터 HUD 카드 생성
        }
    }

    private MemberView CreateMemberView(CharacterData characterData) // 캐릭터 한 명의 좌하단 카드 생성
    {
        GameObject cardObject =
            new GameObject(
                $"Party_{characterData.CharacterId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement)); // 캐릭터 상태 카드 생성

        cardObject.transform.SetParent(
            partyRoot,
            false); // 파티 부모 하위 배치

        RectTransform cardRect =
            cardObject.GetComponent<RectTransform>(); // 카드 RectTransform 조회

        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight); // 초상화와 두 게이지의 공통 프로필 크기

        Image cardImage =
            cardObject.GetComponent<Image>(); // 카드 배경 이미지 조회

        cardImage.color = new Color(0.035f, 0.12f, 0.15f, 0.96f); // 피부 이미지가 없을 때 사용할 청록 배경
        LobbyUiArt.Apply(cardImage, "FormationPanel"); // 기존 청록색 금속 테두리 피부 적용
        cardImage.pixelsPerUnitMultiplier = 3f; // 작은 프로필에 맞춘 얇은 금속 테두리

        cardImage.raycastTarget =
            false; // 탐사 클릭 입력 방해 방지

        LayoutElement layoutElement =
            cardObject.GetComponent<LayoutElement>(); // 카드 LayoutElement 조회

        layoutElement.preferredWidth = CardWidth; // 카드 배치에 사용할 공통 너비
        layoutElement.preferredHeight = CardHeight; // 카드 배치에 사용할 공통 높이

        Image portraitBackdrop = CreateImage(cardObject.transform, "PortraitBackdrop"); // 초상화 뒤의 짙은 구획 생성
        SetRect(portraitBackdrop.rectTransform, PortraitPosition, PortraitSize); // 초상화 영역과 배경 배치 일치
        portraitBackdrop.color = new Color(0.019f, 0.042f, 0.054f, 0.88f); // 초상화 가장자리와 패널을 분리하는 색상

        Image portrait =
            CreateImage(
                cardObject.transform,
                "Portrait"); // 초상화 Image 생성

        SetRect(portrait.rectTransform, PortraitPosition, PortraitSize); // 프로필 왼쪽에 기존 초상화 배치

        portrait.sprite =
            characterData.Portrait; // 캐릭터 초상화 적용

        portrait.preserveAspect = true; // 초상화 비율 유지
        TMP_Text nameText = CreateText(cardObject.transform, "Name", 17f, FontStyles.Bold); // 게이지 위 주요 이름 글자 생성
        nameText.enableAutoSizing = true; // 긴 이름에 맞춘 한 줄 글자 크기 조정
        nameText.fontSizeMin = 11f; // 긴 영어 이름까지 한 줄에 표시할 최소 크기
        nameText.fontSizeMax = 17f; // 짧은 이름의 기본 읽기 크기
        nameText.overflowMode = TextOverflowModes.Ellipsis; // 지나치게 긴 이름의 프로필 경계 보존
        SetRect(nameText.rectTransform, new Vector2(85f, 68f), new Vector2(111f, 24f)); // 초상화 오른쪽 상단 이름 배치
        VitalView health = CreateVitalView(cardRect, "Health", "HealthGaugeIcon", 39f); // 기존 하트 원화의 체력 바 배치
        VitalView mental = CreateVitalView(cardRect, "Mental", "MentalGaugeIcon", 12f); // 기존 정신력 원화의 정신력 바 배치

        Image deathShade =
            CreateImage(
                cardObject.transform,
                "DeathShade"); // 사망 흐림 오버레이 생성

        deathShade.color =
            new Color(
                0f,
                0f,
                0f,
                0.48f); // 초상화 사망 흐림 색상 적용

        SetRect(deathShade.rectTransform, PortraitPosition, PortraitSize); // 초상화 위의 기존 사망 오버레이 영역 배치

        TMP_Text deathText =
            CreateText(
                cardObject.transform,
                "DeathText",
                22f,
                FontStyles.Bold); // 빨간 사망 문구 생성

        deathText.alignment =
            TextAlignmentOptions.Center; // 사망 문구 중앙 정렬

        deathText.color =
            new Color(
                1f,
                0.16f,
                0.16f,
                1f); // 사망 문구 빨간색 적용

        SetRect(deathText.rectTransform, PortraitPosition, PortraitSize); // 초상화 중앙에 사망 문구 배치

        deathText.text =
            "사망"; // 사망 표시 문구 지정

        return new MemberView
        {
            Character = characterData,
            Portrait = portrait,
            NameText = nameText,
            Health = health, // 체력 게이지 참조 저장
            Mental = mental, // 정신력 게이지 참조 저장
            DeathShade = deathShade,
            DeathText = deathText
        }; // 생성 캐릭터 HUD 참조 반환
    }

    private static VitalView CreateVitalView(RectTransform profile, string name, string iconArt, float bottom) // 공통 금속 프레임의 상태 게이지 생성
    { // 게이지 구성 시작
        GameObject rowObject = new GameObject(name, typeof(RectTransform)); // 아이콘과 바를 묶을 상태 행 생성
        RectTransform row = rowObject.GetComponent<RectTransform>(); // 상태 행 사각형 조회
        row.SetParent(profile, false); // 캐릭터 프로필 안에 상태 행 배치
        SetRect(row, new Vector2(82f, bottom), new Vector2(114f, 24f)); // 초상화 오른쪽의 한 줄 게이지 영역

        Image track = CreateImage(row, "Track"); // 비어 있는 양이 보이는 바탕 생성
        SetRect(track.rectTransform, new Vector2(26f, 5f), new Vector2(82f, 14f)); // 금속 프레임 안쪽에 맞춘 트랙 배치
        track.color = new Color(0.025f, 0.045f, 0.06f, 1f); // 남은 양의 채움과 대비되는 짙은 바탕
        Image fill = CreateImage(track.transform, "Fill"); // 잔량을 표현할 채움 이미지 생성
        PreparationUi.Stretch(fill.rectTransform, Vector2.zero, Vector2.up); // 처음 비어 있는 가로 채움 기준 적용

        Image frame = CreateImage(row, "Frame"); // 게이지 위에 얹을 투명 금속 프레임 생성
        SetRect(frame.rectTransform, new Vector2(20f, 0f), new Vector2(94f, 24f)); // 트랙 둘레를 감싸는 프레임 배치
        frame.enabled = BattleUiArt.Apply(frame, "VitalGaugeFrame"); // 기존 체력·정신력 공통 금속 프레임 적용
        Image icon = CreateImage(row, "Icon"); // 게이지 종류를 표시할 왼쪽 아이콘 생성
        SetRect(icon.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 24f)); // 게이지 바 왼쪽의 아이콘 배치
        icon.enabled = BattleUiArt.Apply(icon, iconArt); // 실제 체력 또는 정신력 원화 적용
        icon.preserveAspect = true; // 아이콘 원본 비율 유지

        TMP_Text value = CreateText(row, "Value", 15f, FontStyles.Bold); // 게이지 위의 현재값과 최대값 글자 생성
        value.alignment = TextAlignmentOptions.Center; // 비어 있거나 채워진 바의 중앙에 수치 고정
        SetRect(value.rectTransform, new Vector2(27f, 0f), new Vector2(80f, 24f)); // 아이콘과 양쪽 장식을 제외한 수치 영역
        return new VitalView { Fill = fill, Value = value }; // 갱신에 필요한 게이지 참조 반환
    } // 게이지 구성 종료

    private void RefreshAll() // 출전 파티 HP·정신력·사망 표시 갱신
    {
        if (resultManager == null)
        {
            return;
        }

        for (int index = 0;
             index < memberViews.Count;
             index++)
        {
            MemberView memberView =
                memberViews[index]; // 현재 캐릭터 HUD 조회

            if (memberView == null ||
                memberView.Character == null)
            {
                continue;
            }

            if (!resultManager.TryGetSavedAllyState(
                    memberView.Character,
                    out int currentHealth,
                    out int currentMental,
                    out _)) // 실제 체력·정신력 조회와 사망 횟수 출력 생략
            {
                continue;
            }

            bool isDead =
                currentHealth <= 0; // 현재 사망 여부 계산

            ProjectCLanguageDisplay.SetCaption(memberView.NameText, memberView.Character.DisplayName); // 파티 이름의 현재 언어 표시

            memberView.Portrait.color =
                isDead
                    ? new Color(0.42f, 0.42f, 0.42f, 0.32f)
                    : Color.white; // 사망 시 초상화를 흐리고 회색 처리

            Color infoColor =
                isDead
                    ? new Color(0.58f, 0.58f, 0.58f, 1f)
                    : Color.white; // 사망 상태 정보 글자 색상 결정

            memberView.NameText.color =
                infoColor; // 이름 생존 상태 색상 적용

            memberView.Health.Refresh(currentHealth, memberView.Character.MaxHealth, isDead ? FallenFillColor : HealthFillColor, infoColor); // 실제 체력과 캐릭터 최대값 표시
            memberView.Mental.Refresh(currentMental, BattleMentalRuntime.MaximumMental, isDead ? FallenFillColor : MentalFillColor, infoColor); // 실제 정신력과 게임 최대값 표시

            memberView.DeathShade.gameObject.SetActive(
                isDead); // 사망 초상화 흐림 표시 전환

            memberView.DeathText.gameObject.SetActive(
                isDead); // 빨간 사망 문구 표시 전환
        }
    }

    private void ClearMemberViews() // 기존 파티 HUD 카드 제거
    {
        for (int index = memberViews.Count - 1;
             index >= 0;
             index--)
        {
            MemberView memberView =
                memberViews[index]; // 제거 대상 캐릭터 HUD 조회

            if (memberView != null &&
                memberView.Portrait != null)
            {
                Destroy(
                    memberView.Portrait.transform.parent.gameObject); // 캐릭터 카드 전체 제거
            }
        }

        memberViews.Clear(); // 파티 HUD 참조 목록 초기화
    }

    private static Image CreateImage(
        Transform parent,
        string objectName) // 런타임 Image 생성
    {
        GameObject imageObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)); // UI Image 오브젝트 생성

        imageObject.transform.SetParent(
            parent,
            false); // 지정 부모 하위 배치

        Image image = imageObject.GetComponent<Image>(); // 생성된 이미지 조회
        image.raycastTarget = false; // 표시 전용 이미지의 탐사 클릭 통과
        return image; // 생성 이미지 반환
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        float fontSize,
        FontStyles fontStyle) // 한글 TMP 텍스트 생성
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // TMP 텍스트 오브젝트 생성

        textObject.transform.SetParent(
            parent,
            false); // 지정 부모 하위 배치

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>(); // 생성 TMP 텍스트 조회

        text.font =
            ProjectCFontProvider.KoreanFontAsset; // 프로젝트 공용 한글 폰트 적용

        text.fontSize =
            fontSize; // 글자 크기 적용

        text.fontStyle =
            fontStyle; // 글자 스타일 적용

        text.alignment =
            TextAlignmentOptions.MidlineLeft; // 기본 왼쪽 중앙 정렬

        text.color =
            Color.white; // 기본 흰색 글자 적용

        text.enableAutoSizing = false; // 고정 글자 크기 사용
        text.raycastTarget = false; // 탐사 클릭 입력 방해 방지
        text.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄 상태 정보 유지

        return text; // 생성 텍스트 반환
    }

    private static void SetRect(
        RectTransform rectTransform,
        Vector2 anchoredPosition,
        Vector2 sizeDelta) // 카드 내부 오른쪽 정보 Rect 설정
    {
        rectTransform.anchorMin =
            Vector2.zero; // 카드 왼쪽 아래 기준 설정

        rectTransform.anchorMax =
            Vector2.zero; // 카드 왼쪽 아래 기준 설정

        rectTransform.pivot =
            Vector2.zero; // 왼쪽 아래 Pivot 설정

        rectTransform.anchoredPosition =
            anchoredPosition; // 카드 내부 위치 적용

        rectTransform.sizeDelta =
            sizeDelta; // 카드 내부 크기 적용
    }
}
