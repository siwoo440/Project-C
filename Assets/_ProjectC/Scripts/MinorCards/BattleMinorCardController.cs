using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록과 집합 기능 사용
using UnityEngine; // 랜덤과 로그 기능 사용

public sealed class BattleMinorCardController : IDisposable // 전투 경험치와 마이너 카드 선택 관리자
{
    private const int BattleLongDuration = 999999; // 전투 종료 전까지 유지할 상태 효과 지속값
    private const int BattleLongMaximumStacks = 99; // 마이너 카드 중첩 허용 수

    private readonly BattleSceneSetup battleSceneSetup; // 현재 전투 Scene 정보
    private readonly PlayerLevelRunManager levelManager; // 게임 진행 플레이어 레벨
    private readonly PlayerLevelConfig levelConfig; // 경험치 설정
    private readonly List<MinorCardData> cardPool = new List<MinorCardData>(); // 전체 마이너 카드 풀
    private readonly List<MinorCardData> currentChoices = new List<MinorCardData>(); // 현재 화면에 제시한 카드
    private readonly List<MinorCardData> selectedCards = new List<MinorCardData>(); // 현재 전투에서 선택한 카드
    private readonly HashSet<string> selectedCardIds = new HashSet<string>(StringComparer.Ordinal); // 중복 선택 방지 ID
    private readonly List<IDisposable> subscriptions = new List<IDisposable>(); // 전투 이벤트 구독 토큰
    private bool selectionActive; // 현재 마이너 카드 선택 진행 여부
    private bool disposed; // 관리자 종료 여부

    public IReadOnlyList<MinorCardData> CurrentChoices => currentChoices; // 현재 제시 카드 조회
    public IReadOnlyList<MinorCardData> SelectedCards => selectedCards; // 현재 전투 선택 카드 조회
    public bool SelectionActive => selectionActive; // 선택 화면 활성 여부

    public event Action StateChanged; // 선택 상태 변경 알림

    public BattleMinorCardController(BattleSceneSetup sceneSetup, PlayerLevelRunManager playerLevelManager, PlayerLevelConfig config, IReadOnlyList<MinorCardData> minorCardPool) // 전투 마이너 카드 관리자 생성
    {
        battleSceneSetup = sceneSetup ?? throw new ArgumentNullException(nameof(sceneSetup)); // 전투 Scene 저장
        levelManager = playerLevelManager ?? throw new ArgumentNullException(nameof(playerLevelManager)); // 레벨 관리자 저장
        levelConfig = config ?? throw new ArgumentNullException(nameof(config)); // 레벨 설정 저장

        if (minorCardPool != null) // 카드 풀 존재 확인
        {
            for (int index = 0; index < minorCardPool.Count; index++) // 카드 풀 순회
            {
                MinorCardData cardData = minorCardPool[index]; // 현재 마이너 카드 조회
                if (cardData != null && cardData.IsValidData()) // 유효한 카드 확인
                {
                    cardPool.Add(cardData); // 선택 카드 풀 추가
                }
            }
        }

        BattleEventDispatcher dispatcher = battleSceneSetup.BattleEvents ?? throw new InvalidOperationException("전투 이벤트 발행기가 초기화되지 않았습니다."); // 공용 이벤트 조회
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.CardUsed, HandleCardUsed)); // 카드 사용 경험치 연결
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.TurnStarted, HandleTurnStarted)); // 플레이어 턴 시작 선택 연결
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.BattleEnded, HandleBattleEnded)); // 전투 종료 선택 정리 연결
    }

    public void ProcessCurrentTurn() // 초기 연결 전에 이미 시작된 현재 턴 보정
    {
        if (disposed || battleSceneSetup.BattleTurn == null) // 현재 전투 상태 확인
        {
            return;
        }

        if (battleSceneSetup.BattleTurn.CurrentPhase == BattleTurnPhase.PlayerTurn) // 현재 플레이어 턴 확인
        {
            TryPrepareSelection(); // 이전에 남은 선택권 처리
        }
    }

    public bool TrySelectCard(MinorCardData cardData) // 현재 선택지에서 마이너 카드 선택
    {
        if (disposed || !selectionActive || cardData == null || !currentChoices.Contains(cardData)) // 선택 가능 상태 확인
        {
            return false;
        }

        ApplyCardEffect(cardData); // 선택 카드 효과 즉시 적용
        selectedCards.Add(cardData); // 현재 전투 선택 목록 추가
        selectedCardIds.Add(cardData.MinorCardId); // 현재 전투 재등장 방지
        levelManager.TryConsumeMinorCardChoice(); // 레벨업 선택권 하나 소비
        Debug.Log($"[MinorCard] 선택 - {cardData.DisplayName} / {cardData.TargetType} / {cardData.EffectType} {cardData.EffectValue}"); // 선택 결과 출력

        selectionActive = false; // 현재 선택 종료
        currentChoices.Clear(); // 기존 선택지 제거
        TryPrepareSelection(false); // 남은 선택권이 있으면 다음 선택지 즉시 준비
        StateChanged?.Invoke(); // 최종 선택 상태 갱신
        return true;
    }

    private void HandleCardUsed(BattleEventContext eventContext) // 정상 카드 사용 경험치 처리
    {
        if (disposed || eventContext == null || eventContext.Card == null || eventContext.Phase != BattleTurnPhase.PlayerTurn) // 경험치 지급 조건 확인
        {
            return;
        }

        int experience = levelConfig.CardUsedExperience; // 카드 사용 경험치 조회
        if (experience <= 0) // 경험치 설정 확인
        {
            return;
        }

        int gainedLevels = levelManager.GainExperience(experience); // 플레이어 경험치 누적
        Debug.Log($"[PlayerLevel] 카드 사용 EXP +{experience} / {eventContext.Card.DisplayName} / Lv.{levelManager.Level} EXP {levelManager.CurrentExperience}/{levelManager.RequiredExperience}"); // 경험치 결과 출력
        if (gainedLevels > 0) // 레벨 상승 여부 확인
        {
            Debug.Log($"[PlayerLevel] 레벨업 {gainedLevels}회 / 대기 마이너 카드 선택 {levelManager.PendingMinorCardChoices}회"); // 선택권 누적 출력
        }
    }

    private void HandleTurnStarted(BattleEventContext eventContext) // 턴 시작 마이너 카드 선택 처리
    {
        if (disposed || eventContext == null || eventContext.Phase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 시작 여부 확인
        {
            return;
        }

        TryPrepareSelection(); // 레벨업 선택권이 있으면 선택 화면 준비
    }

    private void HandleBattleEnded(BattleEventContext eventContext) // 전투 종료 선택 화면 정리
    {
        if (disposed) // 관리자 종료 상태 확인
        {
            return;
        }

        selectionActive = false; // 전투 종료 시 선택 비활성화
        currentChoices.Clear(); // 현재 선택지 제거
        StateChanged?.Invoke(); // 화면 닫기 알림
    }

    private void TryPrepareSelection(bool notify = true) // 다음 마이너 카드 선택지 준비
    {
        if (disposed || selectionActive || levelManager.PendingMinorCardChoices <= 0) // 선택 준비 필요 여부 확인
        {
            return;
        }

        BattleTurnRuntime battleTurn = battleSceneSetup.BattleTurn; // 현재 전투 턴 조회
        if (battleTurn == null || battleTurn.IsBattleEnded || battleTurn.CurrentPhase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 상태 확인
        {
            return;
        }

        List<MinorCardData> eligibleCards = new List<MinorCardData>(); // 아직 선택하지 않은 카드 목록
        for (int index = 0; index < cardPool.Count; index++) // 전체 카드 풀 순회
        {
            MinorCardData cardData = cardPool[index]; // 현재 카드 조회
            if (cardData != null && !selectedCardIds.Contains(cardData.MinorCardId)) // 현재 전투 미선택 카드 확인
            {
                eligibleCards.Add(cardData); // 이번 선택 후보 추가
            }
        }

        if (eligibleCards.Count == 0) // 더 이상 선택 가능한 카드 없음 확인
        {
            while (levelManager.PendingMinorCardChoices > 0) // 남은 선택권 정리
            {
                levelManager.TryConsumeMinorCardChoice(); // 사용할 카드가 없으므로 선택권 소비
            }

            Debug.LogWarning("[MinorCard] 현재 전투에서 더 이상 선택할 수 있는 마이너 카드가 없습니다."); // 카드 풀 소진 안내
            selectionActive = false; // 선택 비활성 유지
            currentChoices.Clear(); // 선택지 비우기
            if (notify) // 화면 갱신 요청 확인
            {
                StateChanged?.Invoke(); // 화면 갱신
            }
            return;
        }

        currentChoices.Clear(); // 이전 선택지 초기화
        int optionCount = Mathf.Min(levelConfig.ChoiceOptionCount, eligibleCards.Count); // 실제 제시 카드 수 결정
        for (int index = 0; index < optionCount; index++) // 랜덤 선택지 생성
        {
            int randomIndex = UnityEngine.Random.Range(0, eligibleCards.Count); // 남은 후보 중 랜덤 위치 선택
            currentChoices.Add(eligibleCards[randomIndex]); // 현재 선택지 추가
            eligibleCards.RemoveAt(randomIndex); // 같은 선택지 중복 방지
        }

        selectionActive = currentChoices.Count > 0; // 선택 화면 활성화
        if (notify) // 화면 즉시 갱신 확인
        {
            StateChanged?.Invoke(); // 선택지 변경 알림
        }
    }

    private void ApplyCardEffect(MinorCardData cardData) // 선택 카드 효과 적용
    {
        IReadOnlyList<BattleUnitRuntime> targets = cardData.TargetType == MinorCardTargetType.AllAllies
            ? battleSceneSetup.AllyUnits
            : battleSceneSetup.EnemyUnits; // 카드 대상 진영 결정

        int appliedCount = 0; // 실제 효과 적용 유닛 수
        for (int index = 0; index < targets.Count; index++) // 대상 진영 전체 순회
        {
            BattleUnitRuntime targetUnit = targets[index]; // 현재 대상 조회
            if (targetUnit == null || targetUnit.IsDead) // 생존 대상 확인
            {
                continue;
            }

            if (ApplyToUnit(cardData, targetUnit)) // 단일 유닛 효과 적용
            {
                appliedCount++; // 적용 유닛 수 증가
            }
        }

        Debug.Log($"[MinorCard] {cardData.DisplayName} 효과 적용 대상 {appliedCount}명"); // 전체 적용 결과 출력
    }

    private static bool ApplyToUnit(MinorCardData cardData, BattleUnitRuntime targetUnit) // 단일 유닛 마이너 카드 효과 적용
    {
        switch (cardData.EffectType) // 마이너 카드 효과 분기
        {
            case MinorCardEffectType.IncreaseMaxHealth: // 최대 체력 증가
                return targetUnit.ModifyMaxHealth(cardData.EffectValue) != 0; // 전투 런타임 최대 체력 증가
            case MinorCardEffectType.AttackPowerUp: // 공격력 증가
                return ApplyBattleLongStatus(targetUnit, BattleStatusEffectType.AttackPowerUp, cardData.EffectValue); // 전투 종료까지 공격 증가
            case MinorCardEffectType.PhysicalDefenseUp: // 물리 방어 증가
                return ApplyBattleLongStatus(targetUnit, BattleStatusEffectType.PhysicalDefenseUp, cardData.EffectValue); // 전투 종료까지 물리 방어 증가
            case MinorCardEffectType.PhysicalDefenseDown: // 물리 방어 감소
                return ApplyBattleLongStatus(targetUnit, BattleStatusEffectType.PhysicalDefenseDown, cardData.EffectValue); // 전투 종료까지 물리 방어 감소
            case MinorCardEffectType.MagicalResistanceUp: // 마법 저항 증가
                return ApplyBattleLongStatus(targetUnit, BattleStatusEffectType.MagicalResistanceUp, cardData.EffectValue); // 전투 종료까지 마법 저항 증가
            case MinorCardEffectType.MagicalResistanceDown: // 마법 저항 감소
                return ApplyBattleLongStatus(targetUnit, BattleStatusEffectType.MagicalResistanceDown, cardData.EffectValue); // 전투 종료까지 마법 저항 감소
            default:
                return false;
        }
    }

    private static bool ApplyBattleLongStatus(BattleUnitRuntime targetUnit, BattleStatusEffectType effectType, int value) // 전투 종료까지 유지할 상태 보정 적용
    {
        BattleStatusEffectApplyResult result = targetUnit.ApplyStatusEffect(effectType, value, BattleLongDuration, BattleLongMaximumStacks); // 매우 긴 지속값으로 현재 전투 한정 효과 적용
        return result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked; // 적용 성공 여부 반환
    }

    public void Dispose() // 전투 마이너 카드 관리자 해제
    {
        if (disposed) // 중복 해제 확인
        {
            return;
        }

        disposed = true; // 관리자 종료 상태 저장
        for (int index = 0; index < subscriptions.Count; index++) // 공용 이벤트 구독 순회
        {
            subscriptions[index]?.Dispose(); // 안전하게 구독 해제
        }

        subscriptions.Clear(); // 구독 목록 비우기
        currentChoices.Clear(); // 현재 선택지 비우기
        selectedCards.Clear(); // 전투 선택 기록 비우기
        selectedCardIds.Clear(); // 선택 ID 비우기
        selectionActive = false; // 선택 비활성화
    }
}
