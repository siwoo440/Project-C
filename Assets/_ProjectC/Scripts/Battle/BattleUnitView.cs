using System; // 기본 이벤트 기능 사용
using System.Collections; // 코루틴 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 유니티 포인터 이벤트 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleUnitView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler // 전투 유닛 화면 표시
{ // 클래스 시작
    [Header("기본 표시")] // 기본 표시 구역
    [SerializeField] private Image portraitImage; // 초상화 이미지
    [SerializeField] private TMP_Text nameText; // 유닛 이름 텍스트
    [SerializeField] private Image teamFrameImage; // 진영 테두리 이미지
    [Header("체력 표시")] // 체력 표시 구역
    [SerializeField] private Slider healthSlider; // 체력 게이지
    [SerializeField] private TMP_Text healthText; // 체력 숫자 텍스트
    [SerializeField] private GameObject deadMarker; // 사망 표시 오브젝트
    [Header("진영 색상")] // 진영 색상 구역
    [SerializeField] private Color allyColor = new Color(0.2f, 0.55f, 1f, 1f); // 아군 표시 색상
    [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f); // 적 표시 색상
    private GameObject enemyIntentRoot; // 적 행동 예고 오브젝트
    private TMP_Text enemyIntentText; // 적 행동 예고 텍스트
    private BattleCombatFeedbackView combatFeedbackView; // 전투 결과 피드백 화면
    private BattleUnitMotionView motionView; // 유닛 초상화 움직임 화면
    private Outline targetOutline; // 선택 가능 대상 윤곽선
    private BattleStatusEffectView statusEffectView; // 상태 이상 표시 화면
    private BattleMentalView mentalView; // 정신력 표시 화면
    private Image mentalStateBacklight; // 각성·붕괴 머리 역광 이미지
    private GameObject blockRoot; // 방어도 표시 배경
    private TMP_Text blockText; // 방어도 수치 텍스트
    private GameObject partnerRoot; // 파트너 표시 배경
    private TMP_Text partnerText; // 파트너 상태 텍스트
    private bool statusTooltipHovered; // 상태 상세 대상에 마우스 진입 여부
    public BattleUnitRuntime RuntimeUnit { get; private set; } // 연결된 런타임 유닛
    public event Action<BattleUnitRuntime> Clicked; // 유닛 클릭 이벤트
    public void Bind(BattleUnitRuntime runtimeUnit) // 런타임 유닛 연결
    { // 연결 시작
        if (runtimeUnit == null) // 런타임 유닛 누락 확인
        { // 누락 처리 시작
            Debug.LogError("[BattleUnitView] 연결할 런타임 유닛이 없습니다.", this); // 누락 오류 출력
            return; // 연결 중단
        } // 누락 처리 종료
        Unbind(); // 기존 연결 해제
        RuntimeUnit = runtimeUnit; // 새 런타임 유닛 저장
        RuntimeUnit.HealthChanged += HandleHealthChanged; // 체력 변경 이벤트 등록
        RuntimeUnit.BlockChanged += HandleBlockChanged; // 방어도 변경 이벤트 등록
        RuntimeUnit.DamageTaken += HandleDamageTaken; // 피해 적용 이벤트 등록
        RuntimeUnit.HealthRestored += HandleHealthRestored; // 회복 적용 이벤트 등록
        RuntimeUnit.Died += HandleDied; // 사망 이벤트 등록
        RuntimeUnit.MentalChanged += HandleMentalChanged; // 정신력 변화 이벤트 등록
        EnsureCombatFeedbackView(); // 전투 결과 피드백 준비
        EnsureMotionView(); // 유닛 움직임 화면 준비
        EnsureTargetOutline(); // 대상 선택 윤곽선 준비
        EnsureStatusEffectView(); // 상태 이상 화면 준비
        EnsureMentalView(); // 정신력 화면 준비
        EnsureBlockView(); // 방어도 화면 준비
        ApplyHealthGaugeArt(); // 체력 게이지의 전투 이미지 적용
        statusEffectView.Bind(RuntimeUnit); // 상태 이상 화면 연결
        mentalView.Bind(RuntimeUnit); // 정신력 화면 연결
        ApplyStaticData(); // 고정 표시 정보 적용
        EnsureMentalStateBacklight(); // 정신 상태 머리 역광 준비
        RefreshMentalStateBacklight(); // 초기 정신 상태 역광 반영
        RefreshHealth(); // 체력 표시 갱신
        RefreshBlock(); // 방어도 표시 갱신
        SetEnemyIntent(null); // 행동 예고 초기화
    } // 연결 종료
    private void ApplyHealthGaugeArt() // 실제 체력 게이지 배경 이미지 적용
    { // 체력 게이지 디자인 적용 시작
        if (healthSlider == null) // 체력 게이지 존재 확인
        { // 체력 게이지 누락 처리 시작
            return; // 체력 게이지 디자인 적용 중단
        } // 체력 게이지 누락 처리 종료
        Transform background = healthSlider.transform.Find("Background"); // 체력 게이지 배경 조회
        Image image = background != null ? background.GetComponent<Image>() : null; // 체력 게이지 배경 이미지 조회
        if (image != null) // 체력 배경 존재 확인
        { // 체력 배경 색상 설정 시작
            image.sprite = null; // 기존 프레임 이미지 제거
            image.color = new Color(0.04f, 0.07f, 0.07f, 0.95f); // 비어 있는 체력 배경색 설정
            image.raycastTarget = false; // 체력 배경 입력 차단 해제
        } // 체력 배경 색상 설정 종료
        RectTransform sliderRect = healthSlider.transform as RectTransform; // 체력 게이지 영역 조회
        sliderRect.sizeDelta = new Vector2(165f, 26f); // 하트 프레임을 포함한 게이지 크기 설정
        RectTransform fillArea = healthSlider.transform.Find("Fill Area") as RectTransform; // 실제 체력 채움 영역 조회
        if (fillArea != null) // 체력 채움 영역 존재 확인
        { // 체력 채움 영역 설정 시작
            fillArea.offsetMin = new Vector2(25f, 6f); // 하트 오른쪽부터 체력 표시
            fillArea.offsetMax = new Vector2(-5f, -6f); // 테두리 안쪽 체력 표시
        } // 체력 채움 영역 설정 종료
        Transform existingFrame = healthSlider.transform.Find("HealthGaugeFrame"); // 기존 체력 프레임 조회
        if (existingFrame == null) // 체력 프레임 생성 여부 확인
        { // 체력 프레임 생성 시작
            GameObject frameObject = new GameObject("HealthGaugeFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 하트 체력 프레임 생성
            frameObject.transform.SetParent(healthSlider.transform, false); // 체력 게이지 위에 프레임 연결
            RectTransform frameRect = frameObject.GetComponent<RectTransform>(); // 체력 프레임 영역 조회
            frameRect.anchorMin = Vector2.zero; // 프레임 최소 앵커 설정
            frameRect.anchorMax = Vector2.one; // 프레임 최대 앵커 설정
            frameRect.offsetMin = Vector2.zero; // 프레임 왼쪽 아래 여백 제거
            frameRect.offsetMax = Vector2.zero; // 프레임 오른쪽 위 여백 제거
            Image frameImage = frameObject.GetComponent<Image>(); // 체력 프레임 이미지 조회
            frameImage.enabled = BattleUiArt.Apply(frameImage, "VitalGaugeFrame"); // 생성한 투명 프레임 적용과 누락 처리
            frameImage.raycastTarget = false; // 체력 프레임 입력 차단 해제
            GameObject iconObject = new GameObject("HealthGaugeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 별도 하트 아이콘 생성
            iconObject.transform.SetParent(healthSlider.transform, false); // 체력 바 위에 하트 아이콘 연결
            RectTransform iconRect = iconObject.GetComponent<RectTransform>(); // 하트 아이콘 영역 조회
            iconRect.anchorMin = new Vector2(0f, 0.5f); // 아이콘 왼쪽 중앙 최소 앵커 설정
            iconRect.anchorMax = new Vector2(0f, 0.5f); // 아이콘 왼쪽 중앙 최대 앵커 설정
            iconRect.anchoredPosition = new Vector2(13f, 0f); // 체력 바 왼쪽 아이콘 배치
            iconRect.sizeDelta = new Vector2(28f, 28f); // 체력 아이콘 크기 설정
            Image iconImage = iconObject.GetComponent<Image>(); // 하트 아이콘 이미지 조회
            iconImage.enabled = BattleUiArt.Apply(iconImage, "HealthGaugeIcon"); // 투명 하트 아이콘 적용과 누락 처리
            iconImage.preserveAspect = true; // 하트 아이콘 비율 유지
            iconImage.raycastTarget = false; // 하트 아이콘 입력 차단 해제
        } // 체력 프레임 생성 종료
        if (healthText != null) // 체력 글자 존재 확인
        { // 체력 글자 배치 시작
            healthText.transform.SetParent(healthSlider.transform, false); // 체력 글자를 바 내부로 이동
            healthText.transform.SetAsLastSibling(); // 체력 글자를 프레임 위에 표시
            RectTransform textRect = healthText.rectTransform; // 체력 글자 영역 조회
            textRect.anchorMin = Vector2.zero; // 체력 글자 최소 앵커 설정
            textRect.anchorMax = Vector2.one; // 체력 글자 최대 앵커 설정
            textRect.offsetMin = new Vector2(25f, 0f); // 하트 오른쪽 글자 영역 설정
            textRect.offsetMax = new Vector2(-5f, 0f); // 체력 글자 오른쪽 여백 설정
            healthText.fontSize = 16f; // 바 내부 체력 글자 크기 설정
            healthText.alignment = TextAlignmentOptions.Center; // 체력 글자 중앙 정렬
            healthText.color = Color.white; // 체력 글자 밝은 색상 설정
            healthText.raycastTarget = false; // 체력 글자 입력 차단 해제
        } // 체력 글자 배치 종료
    } // 체력 게이지 디자인 적용 종료
    public void Unbind() // 런타임 유닛 연결 해제
    { // 연결 해제 시작
        if (RuntimeUnit == null) // 기존 연결 여부 확인
        { // 미연결 처리 시작
            return; // 연결 해제 중단
        } // 미연결 처리 종료
        RuntimeUnit.HealthChanged -= HandleHealthChanged; // 체력 변경 이벤트 해제
        RuntimeUnit.BlockChanged -= HandleBlockChanged; // 방어도 변경 이벤트 해제
        RuntimeUnit.DamageTaken -= HandleDamageTaken; // 피해 적용 이벤트 해제
        RuntimeUnit.HealthRestored -= HandleHealthRestored; // 회복 적용 이벤트 해제
        RuntimeUnit.Died -= HandleDied; // 사망 이벤트 해제
        RuntimeUnit.MentalChanged -= HandleMentalChanged; // 정신력 변화 이벤트 해제
        statusEffectView?.Unbind(); // 상태 이상 화면 연결 해제
        mentalView?.Unbind(); // 정신력 화면 연결 해제
        if (mentalStateBacklight != null) // 정신 상태 역광 존재 확인
        { // 정신 상태 역광 숨김 시작
            mentalStateBacklight.gameObject.SetActive(false); // 연결 해제 시 역광 숨김
        } // 정신 상태 역광 숨김 종료
        RuntimeUnit = null; // 런타임 참조 제거
        statusTooltipHovered = false; // 이전 유닛 마우스 상태 초기화
        if (blockRoot != null) // 방어도 화면 존재 확인
        { // 방어도 화면 숨김 시작
            blockRoot.SetActive(false); // 연결 해제 시 방어도 숨김
        } // 방어도 화면 숨김 종료
        if (partnerRoot != null) // 파트너 화면 존재 확인
        { // 파트너 화면 숨김 시작
            partnerRoot.SetActive(false); // 연결 해제 시 파트너 숨김
        } // 파트너 화면 숨김 종료
        if (targetOutline != null) // 대상 윤곽선 존재 확인
        { // 대상 윤곽선 초기화 시작
            targetOutline.enabled = false; // 기존 선택 강조 해제
        } // 대상 윤곽선 초기화 종료
        ResetMotion(); // 유닛 움직임 복구
        if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
        { // 행동 예고 숨김 시작
            enemyIntentRoot.SetActive(false); // 행동 예고 숨김
        } // 행동 예고 숨김 종료
    } // 연결 해제 종료
    public IEnumerator PlayAttackAdvance() // 공격 전진 연출 실행
    { // 공격 전진 시작
        if (motionView == null || RuntimeUnit == null) // 움직임 화면과 유닛 확인
        { // 연출 불가 처리 시작
            yield break; // 공격 전진 종료
        } // 연출 불가 처리 종료
        yield return motionView.PlayAttackAdvance(RuntimeUnit.Team); // 진영별 공격 전진 실행
    } // 공격 전진 종료
    public IEnumerator PlayAttackReturn() // 공격 복귀 연출 실행
    { // 공격 복귀 시작
        if (motionView == null) // 움직임 화면 확인
        { // 연출 불가 처리 시작
            yield break; // 공격 복귀 종료
        } // 연출 불가 처리 종료
        yield return motionView.PlayAttackReturn(); // 초상화 원위치 복귀
    } // 공격 복귀 종료
    public IEnumerator PlayCastAnticipation() // 회복 행동 준비 연출 실행
    { // 회복 준비 시작
        if (motionView == null) // 움직임 화면 확인
        { // 연출 불가 처리 시작
            yield break; // 회복 준비 종료
        } // 연출 불가 처리 종료
        yield return motionView.PlayCastAnticipation(); // 초상화 준비 확대 실행
    } // 회복 준비 종료
    public void PlayHitReaction() // 피격 흔들림 실행
    { // 피격 흔들림 시작
        motionView?.PlayHitReaction(); // 초상화 피격 흔들림 시작
    } // 피격 흔들림 종료
    public void PlayHealReaction() // 회복 확대 실행
    { // 회복 확대 시작
        motionView?.PlayHealReaction(); // 초상화 회복 확대 시작
    } // 회복 확대 종료
    public void ShowStatusFeedback(string message, bool isDebuff) // 상태 이상 결과 피드백 표시
    { // 상태 피드백 시작
        EnsureCombatFeedbackView(); // 전투 결과 피드백 준비
        combatFeedbackView.ShowStatusMessage(message, isDebuff); // 상태 결과 플로팅 문구 표시
    } // 상태 피드백 종료
    public void ShowStatusApplyFeedback(BattleStatusEffectType effectType, BattleStatusEffectApplyResult applyResult) // 상태 적용 결과 피드백 표시
    { // 적용 결과 표시 시작
        string effectName = BattleStatusEffectInstance.GetDisplayName(effectType); // 상태 이상 이름 조회
        if (applyResult == BattleStatusEffectApplyResult.Applied) // 신규 적용 결과 확인
        { // 신규 적용 표시 시작
            ShowStatusFeedback($"{effectName} 적용", BattleStatusEffectInstance.IsDebuffType(effectType)); // 신규 적용 문구 표시
            return; // 적용 결과 표시 종료
        } // 신규 적용 표시 종료
        if (applyResult == BattleStatusEffectApplyResult.Stacked) // 중첩 결과 확인
        { // 중첩 표시 시작
            BattleStatusEffectInstance statusEffect = RuntimeUnit == null ? null : RuntimeUnit.GetStatusEffect(effectType); // 적용된 상태 이상 조회
            int stackCount = statusEffect == null ? 0 : statusEffect.StackCount; // 현재 중첩 수 조회
            ShowStatusFeedback($"{effectName} {stackCount}중첩", BattleStatusEffectInstance.IsDebuffType(effectType)); // 중첩 문구 표시
            return; // 적용 결과 표시 종료
        } // 중첩 표시 종료
        if (applyResult == BattleStatusEffectApplyResult.BlockedByImmunity) // 면역 차단 결과 확인
        { // 면역 표시 시작
            ShowStatusFeedback("면역", false); // 면역 차단 문구 표시
            return; // 적용 결과 표시 종료
        } // 면역 표시 종료
        ShowStatusFeedback("적용 실패", true); // 잘못된 적용 문구 표시
    } // 적용 결과 표시 종료
    public void ResetMotion() // 유닛 움직임 초기화
    { // 움직임 초기화 시작
        motionView?.ResetMotion(); // 초상화 위치와 크기 복구
    } // 움직임 초기화 종료
    public void SetEnemyIntent(BattleEnemyAction action) // 적 행동 예고 표시
    { // 행동 예고 표시 시작
        if (action == null || RuntimeUnit == null || RuntimeUnit.Team != BattleTeam.Enemy || action.Actor != RuntimeUnit) // 표시 행동 유효성 확인
        { // 행동 예고 숨김 시작
            if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
            { // 기존 예고 처리 시작
                enemyIntentRoot.SetActive(false); // 행동 예고 숨김
            } // 기존 예고 처리 종료
            return; // 행동 예고 표시 중단
        } // 행동 예고 숨김 종료
        EnsureEnemyIntentView(); // 행동 예고 화면 준비
        string patternHeader = $"{action.PatternDisplayName} · 패턴 {action.PatternIndex}/{action.PatternCount}"; // 행동 이름과 패턴 순번 문구 계산
        if (action.ActionType == EnemyActionType.ApplyStatusEffect) // 상태 적용 행동 확인
        { // 상태 예고 처리 시작
            string effectName = BattleStatusEffectInstance.GetDisplayName(action.StatusEffectType); // 상태 이상 이름 조회
            bool blockedByImmunity = BattleStatusEffectInstance.IsDebuffType(action.StatusEffectType) && action.Target.HasStatusImmunity; // 면역 차단 예상 여부 계산
            string statusPreview = blockedByImmunity ? "면역 예상" : $"{effectName} {action.Amount} · {action.StatusDuration}T"; // 상태 적용 예고 문구 계산
            enemyIntentText.text = $"{patternHeader}\n행동 {action.ActionOrder} · 속도 {action.ActionSpeed}\n예고: {statusPreview}\n→ {action.Target.DisplayName}"; // 상태 행동 예고 내용 적용
        } // 상태 예고 처리 종료
        else // 공격 행동 처리
        { // 공격 예고 처리 시작
            string damageLabel = action.DamageType == BattleDamageType.Magical ? "마법" : action.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
            int modifiedAmount = action.Actor.ModifyOutgoingDamage(action.Amount); // 정신 상태 포함 원본 피해 계산
            BattleDamageResult previewResult = action.PreviewDamage(); // 대상 방어력 포함 예상 피해 계산
            string sharedText = action.SharedDamageRecipient != null && !action.SharedDamageRecipient.IsDead ? $"\n분담: {action.SharedDamageRecipient.DisplayName} {action.PreviewSharedDamage()}" : string.Empty; // 생존 분담자와 예상 피해 문구 계산
            enemyIntentText.text = $"{patternHeader}\n행동 {action.ActionOrder} · 속도 {action.ActionSpeed}\n예고: {damageLabel} {modifiedAmount} → {previewResult.AppliedDamage}\n→ {action.Target.DisplayName}{sharedText}"; // 분담 피해를 포함한 공격 예고 내용 적용
        } // 공격 예고 처리 종료
        enemyIntentRoot.SetActive(true); // 행동 예고 표시
        enemyIntentRoot.transform.SetAsLastSibling(); // 행동 예고 최상단 배치
    } // 행동 예고 표시 종료
    public void SetTargetable(bool targetable) // 대상 선택 가능 표시
    { // 대상 표시 시작
        if (targetOutline == null || RuntimeUnit == null) // 화면 연결 상태 확인
        { // 연결 없음 처리 시작
            return; // 대상 표시 중단
        } // 연결 없음 처리 종료
        targetOutline.enabled = targetable; // 선택 가능 대상 윤곽선 적용
    } // 대상 표시 종료
    public void SetPartnerMarker(bool isPartner, bool usedCard) // 파트너 표시 적용
    { // 파트너 표시 시작
        if (!isPartner || RuntimeUnit == null || RuntimeUnit.IsDead) // 파트너 표시 조건 확인
        { // 파트너 숨김 시작
            if (partnerRoot != null) // 기존 표시 존재 확인
            { // 기존 표시 숨김 시작
                partnerRoot.SetActive(false); // 파트너 표시 숨김
            } // 기존 표시 숨김 종료
            return; // 파트너 표시 중단
        } // 파트너 숨김 종료
        EnsurePartnerView(); // 파트너 표시 화면 준비
        partnerText.text = usedCard ? "파트너 사용" : "파트너"; // 카드 사용 여부 표시
        partnerRoot.SetActive(true); // 파트너 표시 활성화
    } // 파트너 표시 종료
    private void EnsurePartnerView() // 파트너 표시 화면 생성
    { // 파트너 화면 생성 시작
        if (partnerRoot != null) // 기존 화면 확인
        { // 중복 생성 처리 시작
            return; // 화면 생성 중단
        } // 중복 생성 처리 종료
        partnerRoot = new GameObject("PartnerView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 파트너 배경 생성
        partnerRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform rootRect = partnerRoot.GetComponent<RectTransform>(); // 배경 위치 정보 조회
        rootRect.anchorMin = new Vector2(0f, 1f); // 왼쪽 위 최소 앵커
        rootRect.anchorMax = new Vector2(0f, 1f); // 왼쪽 위 최대 앵커
        rootRect.pivot = new Vector2(0f, 1f); // 왼쪽 위 기준점
        rootRect.anchoredPosition = new Vector2(4f, -76f); // 초상화 위쪽 위치
        rootRect.sizeDelta = new Vector2(76f, 26f); // 파트너 배경 크기
        Image background = partnerRoot.GetComponent<Image>(); // 배경 이미지 조회
        background.color = new Color(0.3f, 0.12f, 0.23f, 0.94f); // 자주색 배경 적용
        background.raycastTarget = false; // 대상 선택 입력 유지
        GameObject textObject = new GameObject("PartnerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 파트너 텍스트 생성
        textObject.transform.SetParent(partnerRoot.transform, false); // 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 위치 정보 조회
        textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커
        textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커
        textRect.offsetMin = Vector2.zero; // 텍스트 아래 여백
        textRect.offsetMax = Vector2.zero; // 텍스트 위 여백
        partnerText = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        partnerText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 적용
        partnerText.fontSize = 14f; // 글자 크기 적용
        partnerText.fontStyle = FontStyles.Bold; // 굵은 글자 적용
        partnerText.alignment = TextAlignmentOptions.Center; // 가운데 정렬 적용
        partnerText.color = new Color(1f, 0.85f, 0.94f, 1f); // 밝은 분홍 글자 적용
        partnerText.raycastTarget = false; // 대상 선택 입력 유지
    } // 파트너 화면 생성 종료
    public void OnPointerClick(PointerEventData eventData) // 유닛 포인터 클릭 처리
    { // 포인터 클릭 처리 시작
        if (RuntimeUnit == null || RuntimeUnit.IsDead) // 유닛 연결과 생존 상태 확인
        { // 클릭 불가 처리 시작
            return; // 포인터 클릭 처리 중단
        } // 클릭 불가 처리 종료
        Clicked?.Invoke(RuntimeUnit); // 유닛 클릭 이벤트 알림
    } // 포인터 클릭 처리 종료
    public void OnPointerEnter(PointerEventData eventData) // 유닛 포인터 진입 처리
    { // 포인터 진입 처리 시작
        statusTooltipHovered = true; // 현재 유닛 마우스 진입 기록
        RefreshStatusTooltip(); // 설정에 맞는 상태 상세 표시 갱신
    } // 포인터 진입 처리 종료
    public void OnPointerExit(PointerEventData eventData) // 유닛 포인터 이탈 처리
    { // 포인터 이탈 처리 시작
        statusTooltipHovered = false; // 현재 유닛 마우스 이탈 기록
        RefreshStatusTooltip(); // 상태 상세 툴팁 숨김
    } // 포인터 이탈 처리 종료
    private void LateUpdate() // 상태 상세 키 입력 변화 감시
    { // 상태 상세 갱신 시작
        if (statusTooltipHovered) // 현재 유닛 마우스 진입 여부 확인
        { // 키 입력 상태 갱신 시작
            RefreshStatusTooltip(); // 임시 상세 키와 상태 변화 반영
        } // 키 입력 상태 갱신 종료
    } // 상태 상세 갱신 종료
    private void RefreshStatusTooltip() // 현재 유닛의 상태 상세 표시 갱신
    { // 상태 상세 표시 갱신 시작
        bool visible = RuntimeUnit != null && RuntimeUnit.StatusEffects.Count > 0 && BattleDisplaySettings.ShowTooltip(GraphicsAudioSettingsRuntime.Current, statusTooltipHovered, BattleDisplaySettings.IsTooltipKeyPressed()); // 상태 존재와 상세 표시 조건 확인
        if (visible == (statusEffectView != null && statusEffectView.IsTooltipVisible)) // 기존 상세 표시 상태 확인
        { // 상태 유지 처리 시작
            return; // 불필요한 상세 갱신 중단
        } // 상태 유지 처리 종료
        if (visible) // 상태 상세 표시 필요 확인
        { // 상태 상세 표시 시작
            statusEffectView?.ShowTooltip(); // 현재 유닛 상태 상세 표시
        } // 상태 상세 표시 종료
        else // 상태 상세 숨김 필요 확인
        { // 상태 상세 숨김 시작
            statusEffectView?.HideTooltip(); // 현재 유닛 상태 상세 숨김
        } // 상태 상세 숨김 종료
    } // 상태 상세 표시 갱신 종료
    private void ApplyStaticData() // 고정 표시 정보 적용
    { // 고정 정보 적용 시작
        if (nameText != null) // 이름 텍스트 존재 확인
        { // 이름 적용 시작
            nameText.text = RuntimeUnit.DisplayName; // 유닛 이름 표시
        } // 이름 적용 종료
        if (portraitImage != null) // 초상화 이미지 존재 확인
        { // 초상화 적용 시작
            Sprite battleArt = RuntimeUnit.CharacterSource != null ? RuntimeUnit.CharacterSource.BattleIllustration : null; // 아군 전투 전용 그림 조회
            Sprite displayArt = battleArt != null ? battleArt : RuntimeUnit.Portrait; // 전용 그림 또는 기존 초상화 선택
            portraitImage.sprite = displayArt; // 유닛 그림 적용
            portraitImage.preserveAspect = displayArt != null; // 그림 원본 비율 유지
            portraitImage.enabled = true; // 초상화 또는 임시 표시 활성화
            Color placeholderColor = RuntimeUnit.Team == BattleTeam.Ally ? allyColor : enemyColor; // 진영별 임시 초상화 색상 계산
            portraitImage.color = displayArt != null ? Color.white : new Color(placeholderColor.r, placeholderColor.g, placeholderColor.b, 0.45f); // 원본 또는 임시 초상화 색상 적용
        } // 초상화 적용 종료
        if (teamFrameImage != null) // 진영 테두리 존재 확인
        { // 진영 색상 적용 시작
            teamFrameImage.color = Color.clear; // 클릭 영역 배경 투명 적용
        } // 진영 색상 적용 종료
    } // 고정 정보 적용 종료
    private void RefreshHealth() // 체력 표시 갱신
    { // 체력 갱신 시작
        if (RuntimeUnit == null) // 런타임 유닛 존재 확인
        { // 미연결 처리 시작
            return; // 체력 갱신 중단
        } // 미연결 처리 종료
        if (healthSlider != null) // 체력 게이지 존재 확인
        { // 게이지 갱신 시작
            healthSlider.minValue = 0f; // 게이지 최소값 적용
            healthSlider.maxValue = RuntimeUnit.MaxHealth; // 게이지 최대값 적용
            healthSlider.value = RuntimeUnit.CurrentHealth; // 게이지 현재값 적용
        } // 게이지 갱신 종료
        if (healthText != null) // 체력 텍스트 존재 확인
        { // 체력 숫자 갱신 시작
            healthText.text = $"{RuntimeUnit.CurrentHealth} / {RuntimeUnit.MaxHealth}"; // 현재 체력 표시
        } // 체력 숫자 갱신 종료
        if (deadMarker != null) // 사망 표시 존재 확인
        { // 사망 표시 갱신 시작
            deadMarker.SetActive(RuntimeUnit.IsDead); // 사망 여부 적용
        } // 사망 표시 갱신 종료
    } // 체력 갱신 종료
    private void HandleHealthChanged(BattleUnitRuntime runtimeUnit) // 체력 변경 이벤트 처리
    { // 체력 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit) // 다른 유닛 이벤트 확인
        { // 다른 유닛 처리 시작
            return; // 체력 이벤트 무시
        } // 다른 유닛 처리 종료
        RefreshHealth(); // 체력 표시 갱신
    } // 체력 이벤트 처리 종료
    private void EnsureBlockView() // 방어도 화면 생성
    { // 방어도 화면 생성 시작
        if (blockRoot != null) // 기존 화면 존재 확인
        { // 기존 화면 처리 시작
            return; // 중복 생성 방지
        } // 기존 화면 처리 종료
        blockRoot = new GameObject("BlockView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 방어도 배경 생성
        blockRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform rootRect = blockRoot.GetComponent<RectTransform>(); // 배경 위치 정보 조회
        rootRect.anchorMin = new Vector2(1f, 1f); // 오른쪽 위 최소 앵커
        rootRect.anchorMax = new Vector2(1f, 1f); // 오른쪽 위 최대 앵커
        rootRect.pivot = new Vector2(1f, 1f); // 오른쪽 위 기준점
        rootRect.anchoredPosition = new Vector2(-4f, -76f); // 초상화 위쪽에 가까운 위치
        rootRect.sizeDelta = new Vector2(76f, 26f); // 방어도 배경 크기
        Image background = blockRoot.GetComponent<Image>(); // 배경 이미지 조회
        background.color = new Color(0.08f, 0.22f, 0.3f, 0.94f); // 푸른색 배경 적용
        background.raycastTarget = false; // 대상 선택 입력 유지
        GameObject textObject = new GameObject("BlockText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 방어도 텍스트 생성
        textObject.transform.SetParent(blockRoot.transform, false); // 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 위치 정보 조회
        textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커
        textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커
        textRect.offsetMin = Vector2.zero; // 텍스트 아래 여백
        textRect.offsetMax = Vector2.zero; // 텍스트 위 여백
        blockText = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        blockText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 적용
        blockText.fontSize = 14f; // 글자 크기 적용
        blockText.fontStyle = FontStyles.Bold; // 굵은 글자 적용
        blockText.alignment = TextAlignmentOptions.Center; // 가운데 정렬 적용
        blockText.color = new Color(0.78f, 0.94f, 1f, 1f); // 밝은 푸른 글자 적용
        blockText.raycastTarget = false; // 대상 선택 입력 유지
    } // 방어도 화면 생성 종료
    private void RefreshBlock() // 방어도 화면 갱신
    { // 방어도 화면 갱신 시작
        if (blockRoot == null || RuntimeUnit == null) // 화면과 유닛 연결 확인
        { // 갱신 불가 처리 시작
            return; // 화면 갱신 중단
        } // 갱신 불가 처리 종료
        blockText.text = $"방어 {RuntimeUnit.CurrentBlock}"; // 현재 방어도 문구 적용
        blockRoot.SetActive(!RuntimeUnit.IsDead && RuntimeUnit.CurrentBlock > 0); // 생존 유닛의 양수 방어도 표시
    } // 방어도 화면 갱신 종료
    private void HandleBlockChanged(BattleUnitRuntime runtimeUnit) // 방어도 변경 처리
    { // 방어도 변경 처리 시작
        if (runtimeUnit == RuntimeUnit) // 연결 유닛 확인
        { // 연결 유닛 처리 시작
            RefreshBlock(); // 방어도 화면 갱신
        } // 연결 유닛 처리 종료
    } // 방어도 변경 처리 종료
    private void HandleDamageTaken(BattleUnitRuntime runtimeUnit, BattleDamageResult damageResult) // 피해 적용 이벤트 처리
    { // 피해 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit || combatFeedbackView == null) // 연결 유닛과 피드백 화면 확인
        { // 다른 유닛 처리 시작
            return; // 피해 이벤트 무시
        } // 다른 유닛 처리 종료
        combatFeedbackView.ShowDamage(damageResult); // 피해 숫자와 강조 표시
        PlayHitReaction(); // 피해 대상 초상화 흔들림 시작
    } // 피해 이벤트 처리 종료
    private void HandleHealthRestored(BattleUnitRuntime runtimeUnit, int appliedHealing) // 회복 적용 이벤트 처리
    { // 회복 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit || combatFeedbackView == null) // 연결 유닛과 피드백 화면 확인
        { // 다른 유닛 처리 시작
            return; // 회복 이벤트 무시
        } // 다른 유닛 처리 종료
        combatFeedbackView.ShowHealing(appliedHealing); // 회복 숫자와 강조 표시
        PlayHealReaction(); // 회복 대상 초상화 확대 시작
    } // 회복 이벤트 처리 종료
    private void HandleDied(BattleUnitRuntime runtimeUnit) // 사망 이벤트 처리
    { // 사망 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit) // 다른 유닛 이벤트 확인
        { // 다른 유닛 처리 시작
            return; // 사망 이벤트 무시
        } // 다른 유닛 처리 종료
        RefreshHealth(); // 사망 표시 갱신
        RefreshMentalStateBacklight(); // 사망 유닛의 역광 표시 갱신
        if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
        { // 행동 예고 숨김 시작
            enemyIntentRoot.SetActive(false); // 사망 유닛 예고 숨김
        } // 행동 예고 숨김 종료
    } // 사망 이벤트 처리 종료
    private void HandleMentalChanged(BattleUnitRuntime runtimeUnit, BattleMentalChangeResult changeResult) // 정신력 변화 피드백 처리
    { // 정신력 피드백 시작
        if (runtimeUnit != RuntimeUnit || !changeResult.WasApplied) // 연결 유닛과 변화 여부 확인
        { // 변화 없음 처리 시작
            return; // 피드백 처리 중단
        } // 변화 없음 처리 종료
        RefreshMentalStateBacklight(); // 정신 상태에 따른 머리 역광 갱신
        if (changeResult.StateStarted) // 특수 상태 시작 확인
        { // 특수 상태 피드백 시작
            bool isCollapse = changeResult.CurrentState == BattleMentalState.Collapse; // 붕괴 여부 계산
            ShowStatusFeedback(isCollapse ? "붕괴 발동" : "각성 발동", isCollapse); // 특수 상태 발동 문구 표시
            return; // 피드백 처리 종료
        } // 특수 상태 피드백 종료
        if (changeResult.StateEnded) // 특수 상태 종료 확인
        { // 특수 상태 종료 피드백 시작
            ShowStatusFeedback("정신력 50 복귀", false); // 정신력 복귀 문구 표시
            return; // 피드백 처리 종료
        } // 특수 상태 종료 피드백 종료
        if (changeResult.AppliedDelta != 0) // 정신력 수치 변화 확인
        { // 정신력 수치 피드백 시작
            string deltaLabel = changeResult.AppliedDelta > 0 ? $"+{changeResult.AppliedDelta}" : changeResult.AppliedDelta.ToString(); // 부호 포함 변화량 문구 계산
            ShowStatusFeedback($"정신력 {deltaLabel}", changeResult.AppliedDelta < 0); // 정신력 변화 문구 표시
        } // 정신력 수치 피드백 종료
    } // 정신력 피드백 종료
    private void EnsureMentalStateBacklight() // 정신 상태의 머리 뒤 역광 준비
    { // 머리 역광 준비 시작
        if (mentalStateBacklight != null || portraitImage == null) // 기존 역광과 초상화 확인
        { // 역광 준비 불필요 처리 시작
            return; // 중복 생성 또는 초상화 누락 방지
        } // 역광 준비 불필요 처리 종료
        GameObject lightObject = new GameObject("MentalStateBacklight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 투명 역광 화면 생성
        lightObject.transform.SetParent(transform, false); // 유닛 화면에 역광 연결
        lightObject.transform.SetSiblingIndex(portraitImage.transform.GetSiblingIndex()); // 캐릭터 초상화 뒤에 역광 배치
        RectTransform lightRect = lightObject.GetComponent<RectTransform>(); // 역광 영역 조회
        RectTransform portraitRect = portraitImage.rectTransform; // 캐릭터 초상화 영역 조회
        lightRect.anchorMin = portraitRect.anchorMin; // 초상화 기준 최소 앵커 설정
        lightRect.anchorMax = portraitRect.anchorMax; // 초상화 기준 최대 앵커 설정
        lightRect.anchoredPosition = portraitRect.anchoredPosition + new Vector2(0f, portraitRect.rect.height * 0.33f); // 캐릭터 머리 뒤 위치 설정
        lightRect.sizeDelta = new Vector2(250f, 180f); // 머리 주위 역광 크기 설정
        mentalStateBacklight = lightObject.GetComponent<Image>(); // 역광 이미지 조회
        mentalStateBacklight.enabled = BattleUiArt.Apply(mentalStateBacklight, "MentalStateBacklight"); // 투명 역광 이미지 적용과 누락 처리
        mentalStateBacklight.raycastTarget = false; // 캐릭터 선택 입력 차단 해제
        lightObject.SetActive(false); // 기본 역광 숨김
    } // 머리 역광 준비 종료
    private void RefreshMentalStateBacklight() // 현재 정신 상태의 머리 역광 갱신
    { // 머리 역광 갱신 시작
        if (mentalStateBacklight == null) // 역광 화면 존재 확인
        { // 역광 화면 없음 처리 시작
            return; // 역광 갱신 중단
        } // 역광 화면 없음 처리 종료
        bool active = RuntimeUnit != null && !RuntimeUnit.IsDead && RuntimeUnit.MentalState != BattleMentalState.Neutral; // 생존한 특수 정신 상태 확인
        mentalStateBacklight.gameObject.SetActive(active); // 현재 정신 상태에 따른 역광 표시
        if (active) // 특수 정신 상태 활성 확인
        { // 정신 상태별 역광 색상 적용 시작
            mentalStateBacklight.color = RuntimeUnit.MentalState == BattleMentalState.Awakening ? new Color(1f, 0.82f, 0.38f, 0.7f) : new Color(1f, 0.18f, 0.23f, 0.7f); // 각성 금빛과 붕괴 붉은빛 구분
        } // 정신 상태별 역광 색상 적용 종료
    } // 머리 역광 갱신 종료
    private void EnsureEnemyIntentView() // 적 행동 예고 화면 준비
    { // 예고 화면 준비 시작
        if (enemyIntentRoot != null) // 기존 예고 화면 확인
        { // 기존 화면 처리 시작
            return; // 중복 생성 중단
        } // 기존 화면 처리 종료
        enemyIntentRoot = new GameObject("EnemyIntent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 행동 예고 배경 생성
        enemyIntentRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform intentRect = enemyIntentRoot.GetComponent<RectTransform>(); // 행동 예고 사각형 조회
        intentRect.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 최소 앵커
        intentRect.anchorMax = new Vector2(0.5f, 1f); // 상단 중앙 최대 앵커
        intentRect.pivot = new Vector2(0.5f, 1f); // 상단 중앙 기준점
        intentRect.anchoredPosition = new Vector2(0f, -6f); // 유닛 내부 상단 위치
        intentRect.sizeDelta = new Vector2(180f, 72f); // 네 줄 행동 예고 크기
        Image intentBackground = enemyIntentRoot.GetComponent<Image>(); // 행동 예고 배경 조회
        intentBackground.color = new Color(0.18f, 0.06f, 0.06f, 0.94f); // 행동 예고 배경색 적용
        intentBackground.raycastTarget = false; // 행동 예고 클릭 차단 해제
        GameObject textObject = new GameObject("IntentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 행동 예고 글자 생성
        textObject.transform.SetParent(enemyIntentRoot.transform, false); // 글자 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 글자 사각형 조회
        textRect.anchorMin = Vector2.zero; // 글자 최소 앵커 적용
        textRect.anchorMax = Vector2.one; // 글자 최대 앵커 적용
        textRect.offsetMin = new Vector2(5f, 3f); // 글자 왼쪽 아래 여백
        textRect.offsetMax = new Vector2(-5f, -3f); // 글자 오른쪽 위 여백
        enemyIntentText = textObject.GetComponent<TextMeshProUGUI>(); // 행동 예고 텍스트 조회
        enemyIntentText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        enemyIntentText.fontSize = 12.5f; // 네 줄 예고 글자 크기
        enemyIntentText.color = new Color(1f, 0.78f, 0.42f, 1f); // 행동 예고 글자색 적용
        enemyIntentText.alignment = TextAlignmentOptions.Center; // 행동 예고 가운데 정렬
        enemyIntentText.textWrappingMode = TextWrappingModes.NoWrap; // 행동 예고 자동 줄바꿈 해제
        enemyIntentText.raycastTarget = false; // 행동 예고 글자 클릭 차단 해제
        enemyIntentRoot.SetActive(false); // 행동 예고 기본 숨김
    } // 예고 화면 준비 종료
    private void EnsureCombatFeedbackView() // 전투 결과 피드백 준비
    { // 피드백 준비 시작
        if (combatFeedbackView != null) // 기존 피드백 화면 확인
        { // 기존 피드백 처리 시작
            return; // 중복 준비 중단
        } // 기존 피드백 처리 종료
        combatFeedbackView = GetComponent<BattleCombatFeedbackView>(); // 기존 피드백 컴포넌트 조회
        if (combatFeedbackView == null) // 피드백 컴포넌트 누락 확인
        { // 피드백 컴포넌트 생성 시작
            combatFeedbackView = gameObject.AddComponent<BattleCombatFeedbackView>(); // 런타임 피드백 컴포넌트 추가
        } // 피드백 컴포넌트 생성 종료
    } // 피드백 준비 종료
    private void EnsureMotionView() // 유닛 움직임 화면 준비
    { // 움직임 준비 시작
        if (motionView == null) // 기존 움직임 컴포넌트 확인
        { // 움직임 컴포넌트 생성 시작
            motionView = GetComponent<BattleUnitMotionView>(); // 기존 움직임 컴포넌트 조회
            if (motionView == null) // 움직임 컴포넌트 누락 확인
            { // 움직임 컴포넌트 추가 시작
                motionView = gameObject.AddComponent<BattleUnitMotionView>(); // 런타임 움직임 컴포넌트 추가
            } // 움직임 컴포넌트 추가 종료
        } // 움직임 컴포넌트 생성 종료
        RectTransform portraitRect = portraitImage == null ? null : portraitImage.rectTransform; // 초상화 사각형 조회
        motionView.Initialize(portraitRect); // 움직임 대상 초상화 연결
    } // 움직임 준비 종료
    private void EnsureTargetOutline() // 선택 가능 대상 윤곽선 준비
    { // 윤곽선 준비 시작
        if (portraitImage == null) // 캐릭터 그림 존재 확인
        { // 그림 누락 처리 시작
            return; // 윤곽선 준비 중단
        } // 그림 누락 처리 종료
        targetOutline = portraitImage.GetComponent<Outline>(); // 기존 윤곽선 조회
        if (targetOutline == null) // 윤곽선 누락 확인
        { // 윤곽선 추가 시작
            targetOutline = portraitImage.gameObject.AddComponent<Outline>(); // 그림 윤곽선 추가
        } // 윤곽선 추가 종료
        targetOutline.effectColor = Color.yellow; // 선택 가능 윤곽선 색상 적용
        targetOutline.effectDistance = new Vector2(3f, -3f); // 윤곽선 두께 적용
        targetOutline.useGraphicAlpha = true; // 그림 투명도 기준 윤곽선 적용
        targetOutline.enabled = false; // 기본 윤곽선 숨김
    } // 윤곽선 준비 종료
    private void EnsureStatusEffectView() // 상태 이상 화면 준비
    { // 상태 화면 준비 시작
        if (statusEffectView != null) // 기존 상태 화면 확인
        { // 기존 화면 처리 시작
            return; // 상태 화면 생성 중단
        } // 기존 화면 처리 종료
        statusEffectView = GetComponent<BattleStatusEffectView>(); // 기존 상태 화면 컴포넌트 조회
        if (statusEffectView == null) // 상태 화면 컴포넌트 누락 확인
        { // 상태 화면 추가 시작
            statusEffectView = gameObject.AddComponent<BattleStatusEffectView>(); // 런타임 상태 화면 컴포넌트 추가
        } // 상태 화면 추가 종료
    } // 상태 화면 준비 종료
    private void EnsureMentalView() // 정신력 화면 준비
    { // 정신력 화면 준비 시작
        if (mentalView != null) // 기존 정신력 화면 확인
        { // 기존 화면 처리 시작
            return; // 중복 준비 중단
        } // 기존 화면 처리 종료
        mentalView = GetComponent<BattleMentalView>(); // 기존 정신력 화면 컴포넌트 조회
        if (mentalView == null) // 정신력 화면 컴포넌트 누락 확인
        { // 컴포넌트 추가 시작
            mentalView = gameObject.AddComponent<BattleMentalView>(); // 런타임 정신력 화면 컴포넌트 추가
        } // 컴포넌트 추가 종료
    } // 정신력 화면 준비 종료
    private void OnDestroy() // 오브젝트 제거 처리
    { // 제거 처리 시작
        Unbind(); // 이벤트 연결 해제
    } // 제거 처리 종료
} // 클래스 종료
