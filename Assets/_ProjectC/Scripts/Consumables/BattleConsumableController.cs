using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 로그 기능 사용

public sealed class BattleConsumableController : IDisposable // 전투 소모품 사용과 슬롯 정리 관리자
{
    private readonly ConsumableInventoryRuntime inventory; // 공용 소모품 보관함
    private readonly BattleSceneSetup battleSceneSetup; // 현재 전투 씬 초기화
    private readonly BattleTurnRuntime battleTurn; // 생성 시점 전투 턴 관리자 고정 참조
    private readonly List<BattleUnitView> unitViews = new List<BattleUnitView>(); // 클릭 가능한 전투 유닛 화면 목록
    private int selectedUseSlot = -1; // 대상 선택 대기 포션 슬롯
    private int selectedMoveSlot = -1; // Alt 정리 원본 슬롯
    private bool disposed; // 관리자 종료 여부

    public int SelectedUseSlot => selectedUseSlot; // 사용 대기 슬롯 조회
    public int SelectedMoveSlot => selectedMoveSlot; // 이동 대기 슬롯 조회
    public event Action SelectionChanged; // 슬롯 선택 상태 변경 이벤트

    public BattleConsumableController(ConsumableInventoryRuntime consumableInventory, BattleSceneSetup sceneSetup) // 전투 소모품 관리자 생성
    {
        inventory = consumableInventory ?? throw new ArgumentNullException(nameof(consumableInventory)); // 소모품 보관함 저장
        battleSceneSetup = sceneSetup ?? throw new ArgumentNullException(nameof(sceneSetup)); // 전투 씬 초기화 저장
        battleTurn = battleSceneSetup.BattleTurn ?? throw new InvalidOperationException("전투 턴 관리자가 초기화되지 않았습니다."); // 생성 시점 턴 관리자 저장
        RegisterUnitViews(); // 전투 유닛 클릭 이벤트 등록
        battleTurn.StateChanged += HandleTurnStateChanged; // 턴 변경 시 선택 상태 정리 연결
    }

    public void HandleSlotClick(int slotIndex, bool rearrangeMode) // 슬롯 클릭 처리
    {
        if (disposed) // 관리자 종료 확인
        {
            return; // 슬롯 클릭 처리 중단
        }

        if (rearrangeMode) // Alt 정리 모드 확인
        {
            HandleRearrangeClick(slotIndex); // 슬롯 이동 또는 교환 처리
            return; // 일반 사용 처리 중단
        }

        CancelMoveSelection(); // Alt 이동 선택 정리
        TryBeginUse(slotIndex); // 포션 사용 또는 대상 선택 시작
    }

    public void CancelMoveSelection() // Alt 이동 선택 취소
    {
        if (selectedMoveSlot < 0) // 이동 선택 존재 확인
        {
            return; // 이동 선택 취소 중단
        }

        selectedMoveSlot = -1; // 이동 원본 슬롯 초기화
        SelectionChanged?.Invoke(); // 선택 상태 변경 알림
    }

    public void CancelUseSelection() // 포션 사용 선택 취소
    {
        if (selectedUseSlot < 0) // 사용 선택 존재 확인
        {
            return; // 사용 선택 취소 중단
        }

        selectedUseSlot = -1; // 대상 선택 포션 초기화
        SelectionChanged?.Invoke(); // 선택 상태 변경 알림
    }

    private void HandleRearrangeClick(int slotIndex) // Alt 슬롯 정리 처리
    {
        CancelUseSelection(); // 전투 사용 선택 취소
        ConsumableItemData clickedItem = inventory.GetItem(slotIndex); // 클릭 슬롯 소모품 조회
        if (selectedMoveSlot < 0) // 이동 원본 미선택 확인
        {
            if (clickedItem == null) // 빈 슬롯 첫 클릭 확인
            {
                return; // 이동 원본 선택 중단
            }

            selectedMoveSlot = slotIndex; // 이동 원본 슬롯 저장
            SelectionChanged?.Invoke(); // 이동 선택 상태 알림
            return; // 대상 슬롯 입력 대기
        }

        if (selectedMoveSlot == slotIndex) // 같은 슬롯 재클릭 확인
        {
            CancelMoveSelection(); // 이동 선택 취소
            return; // 이동 처리 종료
        }

        bool moved = inventory.TryMoveOrSwap(selectedMoveSlot, slotIndex); // 빈칸 이동 또는 점유칸 교환
        Debug.Log($"[Consumable] 슬롯 이동 - {selectedMoveSlot + 1} → {slotIndex + 1} / 성공 {moved}"); // 슬롯 이동 결과 출력
        selectedMoveSlot = -1; // 이동 원본 선택 초기화
        SelectionChanged?.Invoke(); // 이동 선택 상태 알림
    }

    private void TryBeginUse(int slotIndex) // 소모품 사용 시작
    {
        if (!CanUseConsumableNow()) // 전투 사용 가능 시점 확인
        {
            Debug.LogWarning("[Consumable] 소모품은 플레이어 턴에만 사용할 수 있습니다."); // 사용 시점 안내
            return; // 사용 시작 중단
        }

        ConsumableItemData itemData = inventory.GetItem(slotIndex); // 클릭 슬롯 소모품 조회
        if (itemData == null) // 빈 슬롯 확인
        {
            CancelUseSelection(); // 기존 사용 선택 취소
            return; // 빈 슬롯 사용 중단
        }

        PotionData potionData = itemData as PotionData; // 현재 포션 데이터 변환
        if (potionData == null) // 미지원 소모품 종류 확인
        {
            Debug.LogWarning($"[Consumable] 현재 전투 사용이 구현되지 않은 소모품입니다. - {itemData.DisplayName}"); // 미지원 종류 안내
            return; // 미지원 사용 중단
        }

        if (RequiresManualTarget(potionData.TargetType)) // 직접 대상 선택 필요 여부 확인
        {
            selectedUseSlot = slotIndex; // 대상 선택 대기 슬롯 저장
            SelectionChanged?.Invoke(); // 사용 선택 상태 알림
            Debug.Log($"[Consumable] 대상 선택 대기 - 슬롯 {slotIndex + 1} / {potionData.DisplayName}"); // 대상 선택 안내 출력
            return; // 유닛 클릭 입력 대기
        }

        if (TryExecutePotion(slotIndex, potionData, null)) // 즉시 대상 포션 실행 확인
        {
            selectedUseSlot = -1; // 사용 선택 초기화
            SelectionChanged?.Invoke(); // 사용 완료 상태 알림
        }
    }

    private void HandleUnitClicked(BattleUnitRuntime runtimeUnit) // 전투 유닛 클릭 처리
    {
        if (disposed || selectedUseSlot < 0 || runtimeUnit == null) // 대상 선택 상태 확인
        {
            return; // 유닛 클릭 처리 중단
        }

        ConsumableItemData itemData = inventory.GetItem(selectedUseSlot); // 선택 슬롯 현재 소모품 조회
        PotionData potionData = itemData as PotionData; // 선택 소모품 포션 변환
        if (potionData == null) // 선택 소모품 존재 확인
        {
            CancelUseSelection(); // 잘못된 사용 선택 취소
            return; // 대상 처리 중단
        }

        if (!IsValidManualTarget(potionData.TargetType, runtimeUnit)) // 포션 대상 진영 확인
        {
            Debug.LogWarning($"[Consumable] {potionData.DisplayName}의 올바른 대상을 선택해야 합니다."); // 잘못된 대상 안내
            return; // 대상 선택 유지
        }

        int slotIndex = selectedUseSlot; // 사용 슬롯 임시 저장
        if (TryExecutePotion(slotIndex, potionData, runtimeUnit)) // 선택 대상 포션 실행 확인
        {
            selectedUseSlot = -1; // 사용 선택 초기화
            SelectionChanged?.Invoke(); // 사용 완료 상태 알림
        }
    }

    private bool TryExecutePotion(int slotIndex, PotionData potionData, BattleUnitRuntime manualTarget) // 포션 효과 실행과 소비
    {
        if (potionData == null || inventory.GetItem(slotIndex) != potionData) // 포션 슬롯 상태 확인
        {
            return false; // 포션 실행 실패 반환
        }

        List<BattleUnitRuntime> targets = ResolveTargets(potionData.TargetType, manualTarget); // 포션 효과 대상 계산
        if (targets.Count < 1) // 효과 대상 존재 확인
        {
            Debug.LogWarning($"[Consumable] {potionData.DisplayName}의 유효한 대상이 없습니다."); // 대상 없음 안내
            return false; // 포션 실행 실패 반환
        }

        bool appliedAny = false; // 효과 적용 여부 초기화
        for (int index = 0; index < targets.Count; index++) // 포션 대상 순회
        {
            if (ApplyPotionEffect(potionData, targets[index])) // 개별 포션 효과 적용 확인
            {
                appliedAny = true; // 하나 이상 효과 적용 저장
            }
        }

        if (!appliedAny) // 실제 효과 적용 여부 확인
        {
            Debug.LogWarning($"[Consumable] {potionData.DisplayName} 효과가 적용되지 않아 소비하지 않았습니다."); // 미적용 소비 방지 안내
            return false; // 포션 소비 실패 반환
        }

        bool consumed = inventory.TryRemoveAt(slotIndex); // 사용 성공 슬롯만 비우기
        Debug.Log($"[Consumable] 포션 사용 - 슬롯 {slotIndex + 1} / {potionData.DisplayName} / 소비 {consumed}"); // 포션 사용 결과 출력
        return consumed; // 실제 소비 결과 반환
    }

    private static bool ApplyPotionEffect(PotionData potionData, BattleUnitRuntime targetUnit) // 단일 대상 포션 효과 적용
    {
        if (potionData == null || targetUnit == null || targetUnit.IsDead) // 포션과 대상 유효성 확인
        {
            return false; // 효과 적용 실패 반환
        }

        switch (potionData.EffectType) // 포션 효과 종류 분기
        {
            case PotionEffectType.RestoreHealth: // 체력 회복 효과
                return targetUnit.RestoreHealth(potionData.EffectValue) > 0; // 실제 체력 회복 여부 반환
            case PotionEffectType.ChangeMental: // 정신력 변경 효과
                if (!targetUnit.CanChangeMental(potionData.EffectValue)) // 정신력 변경 가능 여부 확인
                {
                    return false; // 정신력 효과 실패 반환
                }
                targetUnit.ChangeMental(potionData.EffectValue, BattleMentalChangeReason.ConsumableEffect); // 소모품 정신력 변경 적용
                return true; // 정신력 효과 성공 반환
            case PotionEffectType.DealDamage: // 피해 효과
                targetUnit.TakeDamage(potionData.EffectValue, potionData.DamageType); // 지정 피해 적용
                return true; // 피해 효과 성공 반환
            case PotionEffectType.CleanseDebuffs: // 디버프 정화 효과
                return targetUnit.RemoveAllDebuffs() > 0; // 실제 디버프 제거 여부 반환
            case PotionEffectType.ApplyStatusEffect: // 상태 이상 효과
                BattleStatusEffectApplyResult applyResult = targetUnit.ApplyStatusEffect(potionData.StatusEffectType, potionData.EffectValue, potionData.StatusDuration, potionData.StatusMaximumStacks); // 지정 상태 효과 적용
                return applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked; // 상태 적용 성공 여부 반환
            default: // 미지원 포션 효과
                return false; // 효과 적용 실패 반환
        }
    }

    private List<BattleUnitRuntime> ResolveTargets(ConsumableTargetType targetType, BattleUnitRuntime manualTarget) // 포션 대상 목록 계산
    {
        List<BattleUnitRuntime> targets = new List<BattleUnitRuntime>(); // 대상 결과 목록 생성
        switch (targetType) // 대상 방식 분기
        {
            case ConsumableTargetType.FirstAlly: // 첫 아군 대상
                AddFirstLiving(battleSceneSetup.AllyUnits, targets); // 첫 생존 아군 추가
                break; // 첫 아군 처리 종료
            case ConsumableTargetType.OneAlly: // 선택 아군 대상
            case ConsumableTargetType.OneEnemy: // 선택 적 대상
                AddLiving(manualTarget, targets); // 수동 선택 대상 추가
                break; // 수동 대상 처리 종료
            case ConsumableTargetType.AllAllies: // 전체 아군 대상
                AddLivingRange(battleSceneSetup.AllyUnits, targets); // 모든 생존 아군 추가
                break; // 전체 아군 처리 종료
            case ConsumableTargetType.AllEnemies: // 전체 적 대상
                AddLivingRange(battleSceneSetup.EnemyUnits, targets); // 모든 생존 적 추가
                break; // 전체 적 처리 종료
        }
        return targets; // 계산 대상 목록 반환
    }

    private bool CanUseConsumableNow() // 전투 소모품 사용 가능 검사
    {
        return battleSceneSetup.IsInitialized && battleTurn != null && !battleTurn.IsBattleEnded && battleTurn.CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 사용 가능 반환
    }

    private static bool RequiresManualTarget(ConsumableTargetType targetType) // 수동 대상 필요 여부 검사
    {
        return targetType == ConsumableTargetType.OneAlly || targetType == ConsumableTargetType.OneEnemy; // 단일 선택 대상 여부 반환
    }

    private static bool IsValidManualTarget(ConsumableTargetType targetType, BattleUnitRuntime runtimeUnit) // 수동 대상 진영 검사
    {
        if (runtimeUnit == null || runtimeUnit.IsDead) // 대상 생존 확인
        {
            return false; // 잘못된 대상 반환
        }

        if (targetType == ConsumableTargetType.OneAlly) // 아군 선택 포션 확인
        {
            return runtimeUnit.Team == BattleTeam.Ally; // 아군 진영 여부 반환
        }

        if (targetType == ConsumableTargetType.OneEnemy) // 적 선택 포션 확인
        {
            return runtimeUnit.Team == BattleTeam.Enemy; // 적 진영 여부 반환
        }

        return false; // 수동 대상 아님 반환
    }

    private void RegisterUnitViews() // 전투 유닛 클릭 이벤트 등록
    {
        BattleUnitView[] foundViews = UnityEngine.Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None); // 현재 전투 유닛 화면 전체 조회
        for (int index = 0; index < foundViews.Length; index++) // 유닛 화면 순회
        {
            BattleUnitView unitView = foundViews[index]; // 현재 유닛 화면 조회
            if (unitView == null || unitViews.Contains(unitView)) // 빈 화면과 중복 등록 확인
            {
                continue; // 다음 유닛 화면 이동
            }

            unitView.Clicked += HandleUnitClicked; // 유닛 클릭 이벤트 연결
            unitViews.Add(unitView); // 등록 화면 목록 추가
        }
    }

    private static void AddFirstLiving(IReadOnlyList<BattleUnitRuntime> sourceUnits, List<BattleUnitRuntime> targets) // 첫 생존 유닛 추가
    {
        for (int index = 0; index < sourceUnits.Count; index++) // 원본 유닛 순회
        {
            BattleUnitRuntime runtimeUnit = sourceUnits[index]; // 현재 유닛 조회
            if (runtimeUnit == null || runtimeUnit.IsDead) // 생존 여부 확인
            {
                continue; // 다음 유닛 이동
            }

            targets.Add(runtimeUnit); // 첫 생존 유닛 추가
            return; // 첫 대상 추가 종료
        }
    }

    private static void AddLivingRange(IReadOnlyList<BattleUnitRuntime> sourceUnits, List<BattleUnitRuntime> targets) // 모든 생존 유닛 추가
    {
        for (int index = 0; index < sourceUnits.Count; index++) // 원본 유닛 순회
        {
            AddLiving(sourceUnits[index], targets); // 현재 생존 유닛 추가
        }
    }

    private static void AddLiving(BattleUnitRuntime runtimeUnit, List<BattleUnitRuntime> targets) // 생존 유닛 단일 추가
    {
        if (runtimeUnit != null && !runtimeUnit.IsDead) // 생존 유닛 확인
        {
            targets.Add(runtimeUnit); // 대상 목록 추가
        }
    }

    private void HandleTurnStateChanged() // 전투 턴 상태 변경 처리
    {
        if (battleTurn == null || battleTurn.CurrentPhase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 이탈 확인
        {
            CancelUseSelection(); // 포션 대상 선택 취소
        }
    }

    public void Dispose() // 전투 소모품 관리자 해제
    {
        if (disposed) // 기존 해제 여부 확인
        {
            return; // 중복 해제 중단
        }

        disposed = true; // 관리자 종료 상태 저장
        if (battleTurn != null) // 저장된 턴 관리자 존재 확인
        {
            battleTurn.StateChanged -= HandleTurnStateChanged; // 턴 변경 연결 해제
        }
        for (int index = 0; index < unitViews.Count; index++) // 등록 유닛 화면 순회
        {
            if (unitViews[index] != null) // 유닛 화면 존재 확인
            {
                unitViews[index].Clicked -= HandleUnitClicked; // 유닛 클릭 연결 해제
            }
        }
        unitViews.Clear(); // 등록 화면 목록 비우기
        selectedUseSlot = -1; // 사용 선택 초기화
        selectedMoveSlot = -1; // 이동 선택 초기화
    }
}
