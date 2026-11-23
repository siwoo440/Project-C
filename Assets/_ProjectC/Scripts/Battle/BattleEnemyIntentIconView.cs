using TMPro; // 행동 아이콘 텍스트 사용
using UnityEngine; // UI 기본 기능 사용
using UnityEngine.EventSystems; // 마우스 오버·클릭 이벤트 사용
using UnityEngine.UI; // 행동 아이콘 이미지 사용

public sealed class BattleEnemyIntentIconView :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler // 적 머리 위 행동 아이콘
{
    private RectTransform rootRect; // 행동 아이콘 RectTransform
    private Image backgroundImage; // 행동 아이콘 이미지
    private TMP_Text iconText; // 행동 아이콘 문자
    private TMP_Text specialMark; // 특수 공격 구분 기호
    private BattleEnemyAction currentAction; // 현재 표시 행동
    private BattleUnitView actorView; // 행동 적 화면
    private BattleEnemyIntentDetailView detailView; // 상세 설명 화면
    private bool specialPattern; // 보스 특수 패턴 여부
    private bool visualCreated; // 아이콘 내부 화면 생성 여부

    private void Awake() // 행동 아이콘 화면 준비
    {
        EnsureVisual(); // 아이콘 내부 화면 생성
    }

    public void Bind(
        BattleEnemyAction action,
        BattleUnitView targetActorView,
        bool isSpecialPattern) // 행동 아이콘 데이터 연결
    {
        EnsureVisual(); // 아이콘 화면 존재 확인

        currentAction = action; // 현재 행동 저장
        actorView = targetActorView; // 행동 적 화면 저장
        specialPattern = isSpecialPattern; // 특수 패턴 상태 저장

        Canvas canvas =
            actorView != null
                ? actorView.GetComponentInParent<Canvas>()
                : null; // 전투 Canvas 조회

        if (canvas != null)
        {
            detailView =
                BattleEnemyIntentDetailView.EnsureInstance(
                    canvas); // 상세 설명 화면 준비
        }

        RefreshVisual(); // 현재 행동 아이콘 갱신

        if (detailView != null)
        {
            detailView.RefreshIfCurrent(
                currentAction,
                actorView,
                specialPattern); // 고정 상세 화면 실시간 수치 갱신
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData) // 행동 아이콘 마우스 오버
    {
        if (currentAction == null ||
            detailView == null)
        {
            return;
        }

        detailView.ShowHover(
            currentAction,
            actorView,
            specialPattern); // 마우스 오버 상세 설명 표시
    }

    public void OnPointerExit(
        PointerEventData eventData) // 행동 아이콘 마우스 이탈
    {
        if (currentAction == null ||
            detailView == null)
        {
            return;
        }

        detailView.HideHover(
            currentAction); // 고정되지 않은 상세 설명 숨김
    }

    public void OnPointerClick(
        PointerEventData eventData) // 행동 아이콘 클릭 고정 처리
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            currentAction == null ||
            detailView == null)
        {
            return;
        }

        detailView.TogglePinned(
            currentAction,
            actorView,
            specialPattern); // 상세 설명 고정·해제
    }

    private void EnsureVisual() // 행동 아이콘 내부 UI 생성
    {
        if (visualCreated)
        {
            return;
        }

        rootRect =
            transform as RectTransform; // 행동 아이콘 RectTransform 조회

        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin =
            new Vector2(
                0.5f,
                1f); // 적 UI 상단 중앙 앵커 설정

        rootRect.anchorMax =
            rootRect.anchorMin; // 앵커 고정

        rootRect.pivot =
            new Vector2(
                0.5f,
                0.5f); // 아이콘 중앙 피벗 설정

        rootRect.anchoredPosition = new Vector2(0f, -58f); // 적 머리 위 중앙에 행동 예고 배치

        rootRect.sizeDelta =
            new Vector2(
                58f,
                58f); // 행동 아이콘 크기 설정

        backgroundImage =
            GetComponent<Image>(); // 행동 아이콘 Image 조회

        if (backgroundImage == null)
        {
            backgroundImage =
                gameObject.AddComponent<Image>(); // 누락 행동 아이콘 Image 추가
        }

        backgroundImage.raycastTarget = true; // 아이콘 마우스 입력 허용

        GameObject textObject =
            new GameObject(
                "IntentIconText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // 행동 아이콘 문자 오브젝트 생성

        textObject.transform.SetParent(
            transform,
            false); // 아이콘에 문자 배치

        iconText =
            textObject.GetComponent<TMP_Text>(); // 행동 아이콘 문자 조회

        iconText.alignment =
            TextAlignmentOptions.Center; // 문자 중앙 정렬

        iconText.fontSize = 15f; // 아이콘 문자 크기 설정
        iconText.fontStyle = FontStyles.Bold; // 아이콘 문자 굵게 설정
        iconText.color = Color.white; // 아이콘 문자 색상 설정
        iconText.raycastTarget = false; // 문자 마우스 입력 차단 해제
        iconText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용

        RectTransform textRect =
            iconText.rectTransform; // 아이콘 문자 RectTransform 조회

        textRect.anchorMin = Vector2.zero; // 문자 최소 앵커 설정
        textRect.anchorMax = Vector2.one; // 문자 최대 앵커 설정
        textRect.offsetMin = Vector2.zero; // 문자 왼쪽 아래 여백 제거
        textRect.offsetMax = Vector2.zero; // 문자 오른쪽 위 여백 제거

        GameObject markObject = new GameObject("SpecialMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 특수 공격 표시 글자 생성
        markObject.transform.SetParent(transform, false); // 행동 아이콘에 특수 표시 연결
        RectTransform markRect = markObject.GetComponent<RectTransform>(); // 특수 표시 사각형 조회
        markRect.anchorMin = new Vector2(0f, 1f); // 특수 표시 왼쪽 위 최소 앵커
        markRect.anchorMax = new Vector2(0f, 1f); // 특수 표시 왼쪽 위 최대 앵커
        markRect.pivot = new Vector2(0f, 1f); // 특수 표시 왼쪽 위 기준점
        markRect.anchoredPosition = Vector2.zero; // 아이콘 왼쪽 위 위치 설정
        markRect.sizeDelta = new Vector2(22f, 24f); // 특수 표시 크기 설정
        specialMark = markObject.GetComponent<TMP_Text>(); // 특수 표시 글자 조회
        specialMark.font = ProjectCFontProvider.KoreanFontAsset; // 특수 표시 글꼴 설정
        specialMark.text = "!"; // 특수 공격 구분 기호 설정
        specialMark.fontSize = 20f; // 특수 공격 기호 크기 설정
        specialMark.fontStyle = FontStyles.Bold; // 특수 공격 기호 굵기 설정
        specialMark.color = new Color(1f, 0.68f, 0.2f, 1f); // 특수 공격 기호 금색 설정
        specialMark.alignment = TextAlignmentOptions.Center; // 특수 공격 기호 중앙 정렬
        specialMark.raycastTarget = false; // 특수 기호의 상세 설명 입력 차단 방지
        specialMark.gameObject.SetActive(false); // 일반 행동의 특수 기호 숨김

        visualCreated = true; // 행동 아이콘 화면 생성 완료
    }

    private void RefreshVisual() // 현재 행동 아이콘 내용 갱신
    {
        if (currentAction == null ||
            backgroundImage == null ||
            iconText == null)
        {
            return;
        }

        int previewAmount =
            GetPreviewAmount(
                currentAction); // 행동 아이콘 표시 수치 계산

        specialMark.gameObject.SetActive(specialPattern && currentAction.ActionType == EnemyActionType.Attack); // 특수 공격의 별도 구분 표시
        if (currentAction.ActionType == EnemyActionType.Attack) // 실제 공격 행동 확인
        { // 공격 유형별 아이콘 표시 시작
            string imageName = currentAction.DamageType == BattleDamageType.Physical ? "EnemyAttackPhysical" : currentAction.DamageType == BattleDamageType.Magical ? "EnemyAttackMagical" : "EnemyAttackNeutral"; // 실제 피해 유형에 따른 아이콘 이름
            bool hasIcon = BattleUiArt.Apply(backgroundImage, imageName); // 투명 공격 유형 아이콘 적용
            backgroundImage.preserveAspect = true; // 사각형 공격 아이콘 비율 유지
            iconText.alignment = hasIcon ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Center; // 아이콘 위 수치 또는 대체 문구 정렬
            iconText.fontSize = hasIcon ? 20f : 15f; // 예상 피해 수치 크기 설정
            iconText.outlineWidth = 0.25f; // 투명 아이콘 위 예상 피해 가독성 설정
            iconText.text = hasIcon ? previewAmount.ToString() : $"ATK\n{previewAmount}"; // 실제 예상 피해 또는 이미지 누락 대체 표시
            if (!hasIcon) // 공격 이미지 누락 확인
            { // 공격 이미지 대체 시작
                backgroundImage.sprite = null; // 기존 행동 이미지 제거
                backgroundImage.color = new Color(0.75f, 0.14f, 0.14f, 0.96f); // 기존 공격 배경색 적용
            } // 공격 이미지 대체 종료
            return; // 공격 유형 표시 완료
        } // 공격 유형별 아이콘 표시 종료
        backgroundImage.sprite = null; // 이전 공격 아이콘 제거
        backgroundImage.preserveAspect = false; // 상태 행동 배경 비율 초기화
        iconText.alignment = TextAlignmentOptions.Center; // 상태 행동 글자 중앙 정렬 복원
        iconText.fontSize = 15f; // 상태 행동 글자 크기 복원
        iconText.outlineWidth = 0f; // 상태 행동 글자 외곽선 복원

        if (specialPattern)
        {
            backgroundImage.color =
                new Color(
                    0.9f,
                    0.35f,
                    0.08f,
                    0.96f); // 보스 특수 패턴 색상

            iconText.text =
                $"!\n{previewAmount}"; // 보스 특수 패턴 아이콘 표시
        }
        else if (currentAction.ActionType == EnemyActionType.ApplyStatusEffect)
        {
            backgroundImage.color =
                new Color(
                    0.55f,
                    0.2f,
                    0.72f,
                    0.96f); // 상태 행동 아이콘 색상

            iconText.text =
                $"FX\n{previewAmount}"; // 상태 행동 아이콘 표시
        }
        else
        {
            backgroundImage.color =
                new Color(
                    0.75f,
                    0.14f,
                    0.14f,
                    0.96f); // 공격 행동 아이콘 색상

            iconText.text =
                $"-\n{previewAmount}"; // 공격이 아닌 기타 행동 표시
        }
    }

    private static int GetPreviewAmount(
        BattleEnemyAction action) // 행동 아이콘 예상 수치 조회
    {
        if (action == null)
        {
            return 0;
        }

        if (action.ActionType == EnemyActionType.Attack)
        {
            BattleDamageResult previewResult =
                action.PreviewDamage(); // 대상 방어 포함 예상 피해 계산

            return previewResult.AppliedDamage; // 예상 실제 피해 반환
        }

        return action.Amount; // 상태 행동 효과 수치 반환
    }
}
