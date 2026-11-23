using System.Collections.Generic; // 파티 카드 UI 목록 사용
using TMPro; // 한글 TMP 텍스트 사용
using UnityEngine; // 런타임 UI와 색상 사용
using UnityEngine.InputSystem; // F7 개발 부활 입력 사용
using UnityEngine.UI; // Canvas와 Image UI 사용

public sealed class ExplorationPartyStatusView : MonoBehaviour // 탐사 좌하단 출전 파티 상태 HUD
{
    private const float RefreshInterval = 0.2f; // 상태 폴링 보조 갱신 간격

    private sealed class MemberView // 캐릭터 한 명의 HUD 참조 모음
    {
        public CharacterData Character; // 표시 캐릭터 데이터
        public Image Portrait; // 캐릭터 초상화
        public TMP_Text NameText; // 캐릭터 이름
        public TMP_Text HealthText; // 현재 체력
        public TMP_Text MentalText; // 현재 정신력
        public Image DeathShade; // 사망 흐림 오버레이
        public TMP_Text DeathText; // 빨간 사망 문구
    }

    private readonly List<MemberView> memberViews =
        new List<MemberView>(); // 현재 출전 파티 HUD 목록

    private BattleResultManager resultManager; // 파티 영구 상태 관리자
    private ExplorationSessionManager sessionManager; // 탐사 완료 상태 관리자
    private GameObject canvasObject; // 런타임 파티 HUD Canvas
    private RectTransform partyRoot; // 좌하단 파티 카드 부모
    private PartyData boundParty; // 현재 표시 중인 파티
    private float nextRefreshTime; // 다음 보조 갱신 시각

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

        sessionManager =
            targetSessionManager; // 탐사 상태 관리자 저장

        if (resultManager != null)
        {
            resultManager.PartyStateChanged += HandlePartyStateChanged; // 파티 상태 변경 즉시 갱신 등록
        }

        EnsureUi(); // 런타임 좌하단 Canvas 준비
        RebuildIfNeeded(); // 현재 파티 기준 HUD 생성
        RefreshAll(); // 최초 상태 표시
    }

    private void Update() // 파티 상태 보조 갱신과 개발 부활 입력
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

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard != null &&
            keyboard.f7Key.wasPressedThisFrame &&
            (sessionManager == null ||
             !sessionManager.IsExplorationCompleted))
        {
            resultManager.ReviveFirstDeadAlly(); // F7 개발 테스트용 첫 사망 파티원 30% 부활
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

        partyRoot.sizeDelta =
            new Vector2(
                780f,
                96f); // 최대 네 명 표시 영역 설정

        HorizontalLayoutGroup layout =
            rootObject.GetComponent<HorizontalLayoutGroup>(); // 가로 파티 배치기 조회

        layout.spacing = 8f; // 캐릭터 카드 간격 설정
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

        cardRect.sizeDelta =
            new Vector2(
                184f,
                92f); // 캐릭터 카드 크기 설정

        Image cardImage =
            cardObject.GetComponent<Image>(); // 카드 배경 이미지 조회

        cardImage.color =
            new Color(
                0.035f,
                0.045f,
                0.065f,
                0.90f); // 어두운 반투명 카드 배경 적용

        cardImage.raycastTarget =
            false; // 탐사 클릭 입력 방해 방지

        LayoutElement layoutElement =
            cardObject.GetComponent<LayoutElement>(); // 카드 LayoutElement 조회

        layoutElement.preferredWidth = 184f; // 카드 고정 폭 설정
        layoutElement.preferredHeight = 92f; // 카드 고정 높이 설정

        Image portrait =
            CreateImage(
                cardObject.transform,
                "Portrait"); // 초상화 Image 생성

        RectTransform portraitRect =
            portrait.rectTransform; // 초상화 RectTransform 조회

        portraitRect.anchorMin =
            new Vector2(0f, 0.5f); // 카드 왼쪽 중앙 기준

        portraitRect.anchorMax =
            new Vector2(0f, 0.5f); // 카드 왼쪽 중앙 기준

        portraitRect.pivot =
            new Vector2(0f, 0.5f); // 왼쪽 중앙 Pivot 설정

        portraitRect.anchoredPosition =
            new Vector2(8f, 0f); // 카드 내부 초상화 위치 설정

        portraitRect.sizeDelta =
            new Vector2(76f, 76f); // 초상화 크기 설정

        portrait.sprite =
            characterData.Portrait; // 캐릭터 초상화 적용

        portrait.preserveAspect = true; // 초상화 비율 유지
        portrait.raycastTarget = false; // 탐사 클릭 방해 방지

        TMP_Text nameText =
            CreateText(
                cardObject.transform,
                "Name",
                15f,
                FontStyles.Bold); // 캐릭터 이름 텍스트 생성

        SetRect(
            nameText.rectTransform,
            new Vector2(90f, 60f),
            new Vector2(88f, 24f)); // 이름 위치 설정

        TMP_Text healthText =
            CreateText(
                cardObject.transform,
                "Health",
                14f,
                FontStyles.Normal); // 현재 체력 텍스트 생성

        SetRect(
            healthText.rectTransform,
            new Vector2(90f, 34f),
            new Vector2(88f, 22f)); // 체력 위치 설정

        TMP_Text mentalText =
            CreateText(
                cardObject.transform,
                "Mental",
                14f,
                FontStyles.Normal); // 현재 정신력 텍스트 생성

        SetRect(
            mentalText.rectTransform,
            new Vector2(90f, 10f),
            new Vector2(88f, 22f)); // 정신력 위치 설정

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

        deathShade.raycastTarget = false; // 탐사 클릭 방해 방지

        RectTransform deathShadeRect =
            deathShade.rectTransform; // 사망 오버레이 RectTransform 조회

        deathShadeRect.anchorMin =
            new Vector2(0f, 0.5f); // 초상화 위치와 동일 기준

        deathShadeRect.anchorMax =
            new Vector2(0f, 0.5f); // 초상화 위치와 동일 기준

        deathShadeRect.pivot =
            new Vector2(0f, 0.5f); // 초상화 위치와 동일 Pivot

        deathShadeRect.anchoredPosition =
            new Vector2(8f, 0f); // 초상화 위 오버레이 배치

        deathShadeRect.sizeDelta =
            new Vector2(76f, 76f); // 초상화와 같은 크기 적용

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

        RectTransform deathRect =
            deathText.rectTransform; // 사망 문구 RectTransform 조회

        deathRect.anchorMin =
            new Vector2(0f, 0.5f); // 초상화 기준 배치

        deathRect.anchorMax =
            new Vector2(0f, 0.5f); // 초상화 기준 배치

        deathRect.pivot =
            new Vector2(0f, 0.5f); // 초상화 기준 Pivot

        deathRect.anchoredPosition =
            new Vector2(8f, 0f); // 초상화 중앙에 사망 문구 배치

        deathRect.sizeDelta =
            new Vector2(76f, 76f); // 초상화 영역 전체 사용

        deathText.text =
            "사망"; // 사망 표시 문구 지정

        return new MemberView
        {
            Character = characterData,
            Portrait = portrait,
            NameText = nameText,
            HealthText = healthText,
            MentalText = mentalText,
            DeathShade = deathShade,
            DeathText = deathText
        }; // 생성 캐릭터 HUD 참조 반환
    }

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
                    out int deathCount))
            {
                continue;
            }

            bool isDead =
                currentHealth <= 0; // 현재 사망 여부 계산

            memberView.NameText.text =
                memberView.Character.DisplayName; // 캐릭터 이름 갱신

            memberView.HealthText.text =
                $"HP {currentHealth}/{memberView.Character.MaxHealth}"; // 현재 체력 표시

            memberView.MentalText.text =
                $"정신 {currentMental}/{BattleMentalRuntime.MaximumMental}"; // 현재 정신력 표시

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

            memberView.HealthText.color =
                infoColor; // 체력 생존 상태 색상 적용

            memberView.MentalText.color =
                infoColor; // 정신력 생존 상태 색상 적용

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

        return imageObject.GetComponent<Image>(); // 생성 Image 반환
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
