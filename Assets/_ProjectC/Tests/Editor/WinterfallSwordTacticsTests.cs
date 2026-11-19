using System.Collections; // 실제 손패와 턴 전환 대기
using System.Linq; // 피해와 상태의 대상 비교
using NUnit.Framework; // 전투 결과 검사
using UnityEngine; // 재현 가능한 전투 난수
using UnityEngine.EventSystems; // 실제 드래그 입력 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 적 초상화의 대상 강조 확인
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 손패 전투 검사 환경 재사용
using Random = UnityEngine.Random; // 실제 게임 확률 난수 사용

public sealed class WinterfallSwordTacticsTests // 탐사 보물 검 카드 여덟 장의 실전 효과 검사
{ // 검 전술 카드 검사 시작
    private static int SeedFor(int percent) // 첫 확률 값이 지정 수치인 난수 시드 조회
    { // 경계 난수 조회 시작
        for (int seed = 0; seed < 5000; seed++) // 실제 게임 난수의 시드 후보 순회
        { // 후보 검사 시작
            Random.InitState(seed); // 같은 카드 충돌 시점의 시드 재현
            if (Random.Range(0, 100) == percent) // 원한 확률 경계인지 확인
            { // 경계 시드 발견 시작
                return seed; // 현재 시드 반환
            } // 경계 시드 발견 종료
        } // 후보 검사 종료
        Assert.Fail("검 카드 확률 경계 시드 없음"); // 불완전한 확률 검사 중단
        return 0; // 검사 실패 뒤 기본 반환값
    } // 경계 난수 조회 종료

    [UnityTest] // 실제 찌르기와 피의 칼끝 검사
    public IEnumerator BleedRefreshesWithoutStackingAndBloodEdgeReadsTheRealTarget() // 출혈 고정 피해와 지속 갱신 확인
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 화면 시작
        yield return CheckBleed(); // 출혈 발생과 두 번의 종료 피해 확인
        yield return new ExitPlayMode(); // 실제 손패 화면 종료
    } // 플레이 검사 종료
    private static IEnumerator CheckBleed() // 실제 보상 원본으로 출혈 공격 사용
    { // 출혈 전투 시작
        using (var fight = new Fight("THRUST", "THRUST", "BLOOD_EDGE", "BLOOD_EDGE")) // 중복 카드와 실제 소유자 준비
        { // 원본 카드 사용 시작
            var target = fight.Enemies[0]; // 출혈과 추가 피해를 확인할 적
            int roll = SeedFor(20); // 이십 퍼센트의 첫 실패 경계
            fight.Controller.CardUsed += (card, targets) => Random.InitState(roll); // 충돌 직전 고정 난수 복원
            yield return fight.Use("THRUST", 0); // 확률 밖의 찌르기 사용
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 정확히 이십일 때 부여 실패
            yield return fight.Use("BLOOD_EDGE", 0); // 출혈 없는 적을 공격
            Assert.AreEqual(187, target.CurrentHealth); // 찌르기 일곱과 기본 피해 여섯
            roll = SeedFor(19); // 이십 퍼센트의 마지막 성공 경계
            yield return fight.Use("THRUST", 0); // 출혈을 일으키는 찌르기 사용
            Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.Bleed).RemainingTurns); // 실제 대상에게 두 턴 출혈
            yield return fight.Use("BLOOD_EDGE", 0); // 출혈 중인 적을 공격
            Assert.AreEqual(170, target.CurrentHealth); // 찌르기 일곱과 출혈 보너스를 더한 열
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 적 턴 시작
            target.GainBlock(12); // 출혈 피해의 방어도 우회 확인
            int blockAtTick = -1; // 턴 종료의 방어도 초기화 전 기록
            target.DamageResolved += (source, unit, damage) => blockAtTick = unit.CurrentBlock; // 고정 체력 피해가 발생한 순간 방어도 보관
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료로 출혈 발동
            Assert.AreEqual(168, target.CurrentHealth); // 방어도를 무시한 체력 두 점 감소
            Assert.AreEqual(12, blockAtTick); // 출혈 타격 자체는 방어도를 소모하지 않음
            Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.Bleed).RemainingTurns); // 한 턴 남은 실제 출혈
            yield return fight.Use("THRUST", 0); // 남은 출혈을 실제 카드로 재부여
            var bleed = target.GetStatusEffect(BattleStatusEffectType.Bleed); // 갱신된 같은 출혈 상태
            Assert.AreEqual(1, bleed.StackCount); // 출혈 피해 중첩 방지
            Assert.AreEqual(2, bleed.RemainingTurns); // 두 턴으로 지속 갱신
            Assert.AreEqual(2, bleed.EffectiveValue); // 갱신 뒤 고정 피해 두 점
            int afterHit = target.CurrentHealth; // 갱신 공격 직후의 적 체력
            for (int turn = 1; turn <= 3; turn++) // 출혈 두 번과 만료 뒤 한 번 비교
            { // 적 턴 진행 시작
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 턴 종료
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 종료로 출혈 판정
                Assert.AreEqual(afterHit - 2 * Mathf.Min(turn, 2), target.CurrentHealth); // 실제 종료 두 번에만 동일한 피해 확인
            } // 적 턴 진행 종료
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 정확히 두 번 뒤 출혈 제거
            target.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 디버프 면역 대상 준비
            yield return fight.Use("THRUST", 0); // 성공 확률의 찌르기를 면역 대상에게 사용
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 출혈의 기존 디버프 면역 호환
        } // 원본 카드 사용 종료
    } // 출혈 전투 종료

    [UnityTest] // 위치와 물리 방어 및 소유자 회피 검사
    public IEnumerator HorizontalSlashHitsTwoFixedSlotsAndDefensiveEffectsExpireOnTime() // 가로베기와 방패 쪼개기 및 견제 참격 확인
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 화면 시작
        yield return CheckPositionAndDefense(); // 고정 범위와 방어 상태의 턴 경계 확인
        yield return new ExitPlayMode(); // 실제 손패 화면 종료
    } // 플레이 검사 종료
    private static IEnumerator CheckPositionAndDefense() // 실제 범위 공격과 방어 변화 사용
    { // 범위 및 방어 검사 시작
        using (var fight = new Fight("HORIZONTAL_SLASH", "HORIZONTAL_SLASH", "HORIZONTAL_SLASH", "SHIELD_SPLITTER", "CHECK_SLASH")) // 고정 영역을 반복 비교할 카드 준비
        { // 손패 사용 시작
            yield return fight.Use("HORIZONTAL_SLASH", 1); // 가운데 적과 바로 오른쪽 적 공격
            CollectionAssert.AreEqual(new[] { 200, 195, 195, 200 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 고정한 두 칸에만 각 한 타격
            yield return fight.Use("HORIZONTAL_SLASH", 3); // 오른쪽 끝과 바로 왼쪽 적 공격
            CollectionAssert.AreEqual(new[] { 200, 195, 190, 195 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 끝 칸의 두 대상에 각 한 타격
            fight.Enemies[2].TakeDamage(999, BattleDamageType.None); // 가운데 인접 칸을 전투불능으로 설정
            yield return fight.Use("HORIZONTAL_SLASH", 1); // 오른쪽 인접 칸이 비어 있는 앵커 공격
            CollectionAssert.AreEqual(new[] { 200, 190, 0, 195 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 죽은 칸을 넘어 다음 적을 공격하지 않음
            var foe = fight.Enemies[0]; // 물리 방어를 낮출 생존 적
            foe.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 3, 6, 1); // 약화 전 실제 물리 방어 수치 준비
            yield return fight.Use("SHIELD_SPLITTER", 0); // 물리 피해를 먼저 준 뒤 적 방어 약화
            Assert.AreEqual(194, foe.CurrentHealth); // 약화 이전 물리 방어를 반영한 피해 여섯
            Assert.AreEqual(2, foe.PhysicalDefense); // 두 턴 동안 물리 방어 한 점 감소
            yield return fight.Use("CHECK_SLASH", 3); // 적에게 피해를 주고 카드 소유자만 회피 증가
            Assert.AreEqual(191, fight.Enemies[3].CurrentHealth); // 선택한 적의 실제 체력 네 점 감소
            Assert.AreEqual(90, BattleHitChance.GetPercent(foe, fight.Allies[0])); // 소유자에 대한 적 명중률 십 퍼센트포인트 감소
            Assert.AreEqual(100, BattleHitChance.GetPercent(foe, fight.Allies[1])); // 다른 아군의 기존 명중률 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 시작
            Assert.AreEqual(90, BattleHitChance.GetPercent(foe, fight.Allies[0])); // 적 공격이 올 때까지 회피 유지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.AreEqual(100, BattleHitChance.GetPercent(foe, fight.Allies[0])); // 첫 적 턴 뒤 회피 종료
            Assert.AreEqual(2, foe.PhysicalDefense); // 첫 적 턴 뒤 방어 약화 한 턴 남음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.AreEqual(3, foe.PhysicalDefense); // 두 번째 적 턴 뒤 원래 방어 복원
        } // 손패 사용 종료
    } // 범위 및 방어 검사 종료

    [UnityTest] // 행동 종류별 취소와 다음 공격 소비 확인
    public IEnumerator KneePommelAndInterceptRespectIntentTypeAndDuration() // 예고 행동과 기절 및 공격 감소 검사
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 화면 시작
        yield return CheckIntent(); // 실제 카드·적 예정 행동 실행
        yield return new ExitPlayMode(); // 실제 손패 화면 종료
    } // 플레이 검사 종료
    private static BattleEnemyAction Intent(BattleUnitRuntime enemy, BattleUnitRuntime ally, EnemyActionType kind) // 동일 적의 공격 또는 상태 행동 원본 구성
    { // 예고 구성 시작
        return new BattleEnemyAction(enemy, ally, kind, BattleDamageType.Physical, 8, kind == EnemyActionType.Attack ? BattleStatusEffectType.None : BattleStatusEffectType.Blind, 1, 1, EnemyTargetRule.FirstLiving, "예고", 1, 2, 4, 0, percentRoll: () => 0); // 실제 전투와 같은 명중·상태·피해 판정
    } // 예고 구성 종료
    private static IEnumerator CheckIntent() // 세 검 카드의 통제 효과 비교
    { // 행동 예고 검사 시작
        using (var fight = new Fight("KNEE_SLASH", "KNEE_SLASH", "POMMEL_STRIKE", "POMMEL_STRIKE", "INTERCEPT_SLASH")) // 실제 행동 제한 카드 다섯 장 준비
        { // 손패 사용 시작
            var foe = fight.Enemies[0]; // 공격과 상태를 번갈아 쓸 적
            var ally = fight.Allies[1]; // 예정 행동의 실제 대상
            var attack = Intent(foe, ally, EnemyActionType.Attack); // 적 공격 예고
            var status = Intent(foe, ally, EnemyActionType.ApplyStatusEffect); // 적 상태 행동 예고
            int roll = SeedFor(25); // 무릎 베기의 실패 경계
            fight.Controller.CardUsed += (card, targets) => Random.InitState(roll); // 충돌 시점의 난수 고정
            yield return fight.Use("KNEE_SLASH", 0); // 이십오 퍼센트 경계 공격
            Assert.IsTrue(status.IsExecutable); // 실패 시 예정 행동 그대로 유지
            roll = SeedFor(24); // 무릎 베기의 성공 경계
            yield return fight.Use("KNEE_SLASH", 0); // 다음 행동 한 번 취소
            Assert.IsFalse(status.IsExecutable); // 비공격 예고도 취소 대상으로 표시
            status.Execute(); // 상태 행동 취소 소비
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 취소한 행동의 상태 부여 차단
            Assert.IsTrue(attack.IsExecutable); // 한 번 소비 뒤 다음 예정 행동 복구
            roll = SeedFor(15); // 검등 치기의 실패 경계
            yield return fight.Use("POMMEL_STRIKE", 0); // 기절 실패 공격
            Assert.IsTrue(attack.IsExecutable); // 십오 퍼센트 경계는 실패
            roll = SeedFor(14); // 검등 치기의 성공 경계
            yield return fight.Use("POMMEL_STRIKE", 0); // 실제 한 턴 기절 부여
            yield return fight.Use("INTERCEPT_SLASH", 0); // 기절 중인 적의 다음 공격도 약화
            Assert.AreEqual(173, foe.CurrentHealth); // 무릎 둘과 검등 둘 및 저지 베기의 기본 피해 합계
            Assert.AreEqual(BattleStatusEffectType.Stun, status.BlockingStatus); // 적 상태 부여 예고도 기절로 차단
            Assert.AreEqual(0, attack.PreviewDamage().AppliedDamage); // 취소된 공격의 최종 예고 영
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 실제 턴 시작
            status.Execute(); // 기절 중 비공격 행동 실행 시도
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 기절 중 부가 상태 없음
            Assert.AreEqual(0, attack.Execute().DamageResult.AppliedDamage); // 같은 턴의 또 다른 공격도 차단
            Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.NextAttackDown)); // 취소한 공격은 약화 소모 없음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 종료로 기절 해제
            Assert.IsTrue(attack.IsExecutable); // 다음 턴의 공격 예고 복원
            status.Execute(); // 공격 전에 실명 부여 행동 실행
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 실제 상태 행동 정상 수행
            Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.NextAttackDown)); // 상태 행동은 다음 공격 약화를 유지
            int weakened = attack.AttackAmount; // 다음 공격 예고가 보여 줄 원본 수치
            Assert.AreEqual(attack.Amount - 2, weakened); // 원래 공격 수치에서 두 점 감소
            Assert.AreEqual(weakened, attack.Execute().DamageResult.RawDamage); // 실제 공격과 예고가 같은 원본 수치
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.NextAttackDown)); // 실행한 공격 한 번 뒤 약화 해제
            Assert.AreEqual(attack.Amount, attack.AttackAmount); // 두 번째 공격 수치 정상 복원
        } // 손패 사용 종료
    } // 행동 예고 검사 종료

    [UnityTest] // 실제 손패의 드래그 입력 경계 검사
    public IEnumerator FinisherRejectsInvalidDropAndCanImmediatelyDragOntoTheBoundary() // 부적격 드롭 뒤 유효 적에게 재드래그 확인
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 입력 컴포넌트 시작
        yield return CheckFinisherDrag(); // 정확한 체력 경계의 강조와 드롭 확인
        yield return new ExitPlayMode(); // 실제 입력 컴포넌트 종료
    } // 플레이 검사 종료
    private static BattleUnitView CreateBattlePortrait(Transform parent, BattleUnitRuntime unit) // 실제 전투 적 초상화 프리팹 생성
    { // 실물 초상화 준비 시작
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 전투 씬에서 사용하는 유닛 원본
        var portrait = Object.Instantiate(prefab, parent).GetComponent<BattleUnitView>(); // 방어도와 대상 윤곽이 있는 실물 화면
        portrait.Bind(unit); // 검사 전투 유닛의 체력과 클릭 연결
        return portrait; // 실제 초상화와 입력 컴포넌트 반환
    } // 실물 초상화 준비 종료
    private static IEnumerator CheckFinisherDrag() // 최후의 일격 드래그의 실제 비용과 대상 확인
    { // 드래그 검사 시작
        using (var fight = new Fight(CreateBattlePortrait, "FINAL_STRIKE")) // 실물 적 초상화와 행동력 전체를 쓰는 검 카드 준비
        { // 대상 조작 시작
            fight.Enemies[0].TakeDamage(159, BattleDamageType.None); // 체력 이십 퍼센트 바로 위인 마흔하나
            fight.Enemies[1].TakeDamage(160, BattleDamageType.None); // 체력 이십 퍼센트 경계인 마흔
            var card = fight.View("FINAL_STRIKE"); // 실제 손패의 최후의 일격
            var drag = card.GetComponent<BattleCardDragHandler>(); // 기존 손패 드래그 입력 조회
            if (drag == null) // 독립 손패의 입력 설치 전인지 확인
            { // 드래그 연결 시작
                drag = card.gameObject.AddComponent<BattleCardDragHandler>(); // 정상 설치기와 같은 입력 요소 연결
                drag.Initialize(card); // 드래그와 카드 원본 연결
            } // 드래그 연결 종료
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 마우스 왼쪽 드래그 입력 준비
            card.OnPointerClick(pointer); // 클릭으로 먼저 선택된 카드 재현
            Assert.AreSame(card.RuntimeCard, fight.Controller.SelectedCard); // 이전 선택 상태 확인
            drag.OnBeginDrag(pointer); // 실제 드래그 시작과 대상 강조
            Assert.IsFalse(fight.EnemyViews[0].GetComponentInChildren<Outline>(true).enabled); // 마흔하나인 적의 강조 제외
            Assert.IsTrue(fight.EnemyViews[1].GetComponentInChildren<Outline>(true).enabled); // 마흔인 적의 강조 포함
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = fight.EnemyViews[0].gameObject }; // 부적격한 적의 실제 화면 입력
            drag.OnEndDrag(pointer); // 유효하지 않은 적 위에 내려놓기
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 부적격 드롭의 비용 보존
            Assert.IsNull(fight.Controller.SelectedCard); // 다음 드래그를 방해할 선택 해제
            drag.OnBeginDrag(pointer); // 같은 손패를 즉시 다시 드래그
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = fight.EnemyViews[1].gameObject }; // 경계에 맞는 적 화면 입력
            drag.OnEndDrag(pointer); // 적 위에 놓아 정상 전투 사용
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 실제 남은 행동력 전부 지불
            float deadline = Time.realtimeSinceStartup + 10f; // 타격 연출 대기 한도
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 타격 충돌 대기
            { // 연출 대기 시작
                yield return null; // 다음 전투 프레임 대기
            } // 연출 대기 종료
            Assert.IsTrue(fight.Enemies[1].IsDead); // 정확히 이십 퍼센트였던 적 처치
            Assert.AreEqual(41, fight.Enemies[0].CurrentHealth); // 유효하지 않았던 적 체력 유지
        } // 대상 조작 종료
    } // 드래그 검사 종료
} // 검 전술 카드 검사 종료
