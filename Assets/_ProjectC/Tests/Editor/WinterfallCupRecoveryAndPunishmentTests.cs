using System.Collections; // 플레이 모드 단계별 검사 사용
using System.Linq; // 실제 아군 전투 화면 찾기 사용
using NUnit.Framework; // 테스트 결과 확인 사용
using UnityEngine; // 카드 원본과 적 자료 생성 사용
using UnityEngine.TestTools; // 테스트의 실제 플레이 모드 전환 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 손패 검사 환경

public sealed class WinterfallCupRecoveryAndPunishmentTests // 해독과 언데드 피해 및 두 가지 성배 회복 검사
{ // 성배 카드 검사 시작
    [UnityTest] // 정화 회복과 진영별 범위 타격 검사 지정
    public IEnumerator DetoxAndPunishmentUseTheirActualPartyAndEnemyTargets() // 정화 범위와 불사형의 개별 피해 검사
    { // 범위 성배 카드 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 준비
        yield return CheckDetoxAndPunishment(); // 도메인 전환 뒤 실제 범위 효과 확인
        yield return new ExitPlayMode(); // 범위 카드 전투 중지
    } // 범위 성배 카드 플레이 모드 종료

    [UnityTest] // 현재 체력 비용과 중단 및 지정 회복 검사 지정
    public IEnumerator SacrificialHealingRequiresSevenOwnerHealthAndPaysBeforeHealing() // 소유자 체력 대가와 자기 대상 가능 여부 확인
    { // 희생 치유 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 손패와 전투 입출력 준비
        yield return CheckSacrificialHealing(); // 도메인 전환 뒤 실제 비용 테스트 실행
        yield return new ExitPlayMode(); // 희생 치유 전투 중지
    } // 희생 치유 플레이 모드 종료

    [UnityTest] // 즉시와 다음 실제 아군 턴의 회복 분리 검사 지정
    public IEnumerator WaterOfLifeResolvesItsDelayedHealingOnceAtNextAllyTurn() // 적 턴 건너뛰기와 다음 아군 턴의 일회성 회복 확인
    { // 생명수 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 카드 입력과 턴 시작 준비
        yield return CheckWaterOfLife(); // 도메인 전환 뒤 예약 효과 테스트 실행
        yield return new ExitPlayMode(); // 예약 회복 전투 중지
    } // 생명수 플레이 모드 종료

    private static IEnumerator CheckDetoxAndPunishment() // 실제 다중 생존 대상과 불사형 예외 피해 검사
    { // 범위 성배 카드 검사 시작
        EnemyData normal = CreateEnemy(false, 0); // 마법 저항이 없는 일반 적 원본 준비
        EnemyData undead = CreateEnemy(true, 0); // 마법 저항이 없는 불사형 적 원본 준비
        EnemyData resistant = CreateEnemy(true, 100); // 기본 피해와 추가 피해를 모두 반으로 줄이는 불사형 준비
        try // 검사 완료와 실패 모두에서 원본 정리 보장
        { // 실제 카드와 혼합 적 검사 시작
            using var fight = Fight.WithEnemySources(new[] { normal, undead, resistant }, "DETOX_RITUAL", "PUNISHMENT_OF_LIGHT"); // 혼합 적 명단과 실제 보물 원본으로 전투 구성
            CardInstance detox = fight.View("DETOX_RITUAL").RuntimeCard; // 실제 해독 의식 보물 카드 조회
            CardInstance punishment = fight.View("PUNISHMENT_OF_LIGHT").RuntimeCard; // 실제 빛의 징벌 보물 카드 조회
            Assert.AreEqual(CardTargetType.AllAllies, detox.TargetType); // 해독의 아군 전체 대상 확인
            Assert.AreEqual(CardEffectType.Heal, detox.EffectType); // 해독의 직접 회복 처리 확인
            Assert.AreEqual(2, detox.ApCost); // 해독 의식의 행동력 비용 확인
            Assert.AreEqual(4, detox.EffectValue); // 해독 의식의 기본 회복량 확인
            Assert.AreEqual(BattleStatusEffectType.Poison, detox.SourceData.StatusRemovedByHealing); // 다른 약화와 구별된 중독 제거 확인
            BattleUnitRuntime wounded = fight.Allies[0]; // 실제 회복을 받을 부상 아군
            BattleUnitRuntime full = fight.Allies[1]; // 이미 체력이 가득 찬 중독 아군
            BattleUnitRuntime fallen = fight.Allies[2]; // 범위 대상에서 제외할 전투불능 아군
            wounded.TakeDamage(10, BattleDamageType.None); // 해독 전의 실제 부상 만들기
            int before = wounded.CurrentHealth; // 해독 전 부상 아군의 체력 저장
            wounded.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1); // 실제 중독 상태 부여
            wounded.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 유지해야 할 별개 출혈 부여
            full.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1); // 최대 체력이어도 제거할 중독 부여
            fallen.TakeDamage(1000, BattleDamageType.None); // 부활하지 않아야 할 아군 실제 사망
            yield return fight.Use("DETOX_RITUAL"); // 실제 손패로 생존 아군 전체 대상 카드 사용
            Assert.IsNull(wounded.GetStatusEffect(BattleStatusEffectType.Poison)); // 부상 대상의 중독 제거 확인
            Assert.IsNull(full.GetStatusEffect(BattleStatusEffectType.Poison)); // 최대 체력 아군의 중독 제거 확인
            Assert.IsNotNull(wounded.GetStatusEffect(BattleStatusEffectType.Bleed)); // 다른 종류의 기존 출혈 보존 확인
            Assert.AreEqual(before + 4, wounded.CurrentHealth); // 부상 아군에 실제 체력 사 점 회복 확인
            Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 가득 찬 아군의 체력 상한 확인
            Assert.IsTrue(fallen.IsDead); // 전투불능 아군 회복과 부활 제외 확인
            Assert.AreEqual(CardTargetType.AllEnemies, punishment.TargetType); // 징벌의 적 전체 대상 확인
            Assert.AreEqual(2, punishment.ApCost); // 빛의 징벌의 행동력 비용 확인
            Assert.AreEqual(BattleDamageType.Magical, punishment.DamageType); // 실제 마법 피해 유형 확인
            Assert.AreEqual(8, punishment.EffectValue); // 징벌의 기본 피해량 확인
            Assert.AreEqual(4, punishment.SourceData.SecondaryEffectValue); // 징벌의 불사형 추가 피해량 확인
            yield return fight.Use("PUNISHMENT_OF_LIGHT"); // 마법 피해를 가진 실제 성배 범위 카드 사용
            Assert.AreEqual(192, fight.Enemies[0].CurrentHealth); // 일반 적에게 마법 피해 팔 적용 확인
            Assert.AreEqual(188, fight.Enemies[1].CurrentHealth); // 불사형 적에게 마법 피해 열둘 적용 확인
            Assert.AreEqual(194, fight.Enemies[2].CurrentHealth); // 저항이 기본과 추가 피해를 모두 감소하는지 확인
        } // 실제 카드와 혼합 적 검사 종료
        finally // 임시 적 원본 정리 시작
        { // 검사 원본 제거 시작
            Object.Destroy(normal); // 일반 적 임시 원본 파기
            Object.Destroy(undead); // 불사형 적 임시 원본 파기
            Object.Destroy(resistant); // 저항 적 임시 원본 파기
        } // 검사 원본 제거 종료
    } // 실제 범위 성배 카드 검사 종료

    private static IEnumerator CheckSacrificialHealing() // 소유자 비용의 생존 경계와 지정 회복 확인
    { // 희생 치유의 비용과 효과 검사 시작
        using (var fight = new Fight("SACRIFICIAL_HEALING", "SACRIFICIAL_HEALING")) // 서로 다른 지정 대상으로 쓸 실제 보물 두 장
        { // 체력 비용 후보 검사 시작
            BattleCardView view = fight.View("SACRIFICIAL_HEALING"); // 첫 희생 치유의 실제 손패 화면
            CardInstance card = view.RuntimeCard; // 실제 카드 인스턴스의 규칙 검사
            BattleUnitRuntime owner = card.OwnerUnit; // 생존 체력 대가의 실제 소유자
            BattleUnitRuntime target = fight.Allies[1]; // 비용을 지불하지 않고 회복만 받을 지정 아군
            owner.ApplyPersistentHealth(6); // 비용과 같은 체력의 사용 불가 경계 준비
            owner.GainBlock(9); // 대가에 소모되지 않을 방어도 준비
            target.ModifyMaxHealth(100 - target.MaxHealth); // 과잉 회복 없이 테스트할 최대 체력 설정
            target.ApplyPersistentHealth(50); // 정량 회복을 확인할 부상 아군 준비
            fight.Hand.RefreshCardAvailability(); // 실제 카드 선택 가능 비용 재평가
            Assert.AreEqual(CardTargetType.SingleAlly, card.TargetType); // 희생 치유의 단일 아군 대상 확인
            Assert.AreEqual(2, card.ApCost); // 희생 치유의 행동력 비용 확인
            Assert.AreEqual(20, card.EffectValue); // 희생 치유의 원본 회복량 확인
            Assert.AreEqual(6, card.SourceData.SecondaryEffectValue); // 희생 치유의 원본 체력 대가 확인
            view.OnPointerClick(null); // 체력 육인 소유자의 실제 손패 입력 시도
            FindView(fight, target).OnPointerClick(null); // 지불할 수 없는 상태에서 아군 대상 선택 시도
            Assert.IsNull(fight.Controller.SelectedCard); // 생존 불가인 카드의 선택 차단 확인
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 행동력 차감 없이 체력 비용 부족 처리 확인
            CollectionAssert.Contains(fight.Deck.Hand, card); // 비용 부족 카드가 손패에 남는지 확인
            Assert.AreEqual(1, owner.RestoreHealth(1)); // 체력 일곱의 사용 가능 경계 만들기
            int ownerHealthAtUse = -1; // 비용이 이미 적용된 뒤 읽을 소유자 체력
            int damageEvents = 0; // 체력 지불을 일반 피격으로 처리하는지 확인할 횟수
            fight.Controller.CardUsed += (_, _) => ownerHealthAtUse = owner.CurrentHealth; // 실제 효과 적용 직전의 소유자 체력 기록
            owner.DamageTaken += (_, _) => damageEvents++; // 체력 대가가 피해 이벤트를 발행하는지 확인
            yield return PlayAllyCard(fight, "SACRIFICIAL_HEALING", target); // 실제 성배를 부상 아군에게 지정해 사용
            Assert.AreEqual(1, ownerHealthAtUse); // 회복 효과 시작 전 이미 체력 육 지불 확인
            Assert.AreEqual(1, owner.CurrentHealth); // 실제 시전자의 한 점 생존 확인
            Assert.AreEqual(9, owner.CurrentBlock); // 실제 방어도 손실 없이 비용 지불 확인
            Assert.AreEqual(0, damageEvents); // 체력 지불을 피격이나 보호력 소모로 기록하지 않는지 확인
            Assert.AreEqual(70, target.CurrentHealth); // 별개 지정 아군에게 체력 스무 점 회복 확인

            owner.RestoreHealth(owner.MaxHealth); // 두 번째 자가 회복을 위해 소유자를 최대 체력으로 복원
            yield return PlayAllyCard(fight, "SACRIFICIAL_HEALING", owner); // 자기 자신을 실제 성배 대상으로 선택
            Assert.AreEqual(owner.MaxHealth - 6, ownerHealthAtUse); // 최대 체력 자가 대상이어도 비용을 먼저 차감하는지 확인
            Assert.AreEqual(owner.MaxHealth, owner.CurrentHealth); // 자가 대상은 비용을 낸 뒤 최대 체력까지만 회복
        } // 체력 비용 후보 검사 종료
    } // 희생 치유 검사 종료

    private static IEnumerator CheckWaterOfLife() // 실제 아군 턴과 적 턴의 예약 회복 시점 검사
    { // 생명수의 두 회복 경로 검사 시작
        using (var fight = new Fight("WATER_OF_LIFE", "WATER_OF_LIFE", "WATER_OF_LIFE", "WATER_OF_LIFE")) // 단일·중첩·전투불능 예약을 위한 실제 카드 네 장
        { // 예약 대상별 검사 시작
            CardInstance card = fight.View("WATER_OF_LIFE").RuntimeCard; // 실제 큐에 들어갈 생명수 원본의 수치 조회
            Assert.AreEqual(2, card.ApCost); // 생명수의 행동력 비용 확인
            Assert.AreEqual(10, card.EffectValue); // 카드의 즉시 체력 회복량 확인
            Assert.AreEqual(6, card.SourceData.SecondaryEffectValue); // 다음 아군 턴의 별개 체력 회복량 확인
            StringAssert.Contains("회복", BattleStatusEffectInstance.GetDisplayName(BattleStatusEffectType.DelayedHealing)); // 예약 아이콘이 실제 회복 이름을 갖는지 확인
            BattleUnitRuntime doubled = fight.Allies[1]; // 같은 턴에 두 번 예약할 부상 아군 준비
            BattleUnitRuntime full = fight.Allies[0]; // 처음 시전할 때 체력이 가득 찬 자기 대상 준비
            BattleUnitRuntime fallen = fight.Allies[2]; // 예약 후 쓰러질 아군 준비
            doubled.ModifyMaxHealth(100 - doubled.MaxHealth); // 효과가 상한에 걸리지 않는 체력 크기 준비
            doubled.ApplyPersistentHealth(20); // 즉시 이십 점과 다음 턴 회복을 나눠 측정할 체력 준비
            yield return PlayAllyCard(fight, "WATER_OF_LIFE", doubled); // 선택한 아군의 첫 직접 회복과 예약 생성
            Assert.AreEqual(30, doubled.CurrentHealth); // 최초 카드의 즉시 회복 십 확인
            Assert.AreEqual(6, doubled.GetStatusEffect(BattleStatusEffectType.DelayedHealing)?.EffectiveValue); // 다음 턴의 회복 예약과 실제 수치 확인
            yield return PlayAllyCard(fight, "WATER_OF_LIFE", doubled); // 같은 아군의 두 번째 직접 회복과 독립 예약
            Assert.AreEqual(40, doubled.CurrentHealth); // 두 번째 직접 회복 십만 즉시 적용 확인
            Assert.AreEqual(12, doubled.GetStatusEffect(BattleStatusEffectType.DelayedHealing)?.EffectiveValue); // 같은 턴 두 예약의 합산 확인
            yield return PlayAllyCard(fight, "WATER_OF_LIFE", full); // 지금은 최대 체력인 아군에게 예약 설치
            Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 즉시 회복의 최대 체력 제한 확인
            yield return PlayAllyCard(fight, "WATER_OF_LIFE", fallen); // 다음 차례 전에 전투불능이 될 별개 아군 예약
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴으로 전환
            Assert.AreEqual(40, doubled.CurrentHealth); // 아군 턴 종료나 적 턴 시작에 예약 미발동 확인
            full.TakeDamage(9, BattleDamageType.None); // 적 턴 중 실제 회복이 필요한 부상 만들기
            int fullBefore = full.CurrentHealth; // 다음 턴 회복을 분리할 적 턴 피해 후 체력 저장
            fallen.TakeDamage(1000, BattleDamageType.None); // 다음 턴 전에 쓰러진 예약 대상 만들기
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴 시작과 상태 처리
            Assert.AreEqual(52, doubled.CurrentHealth); // 두 예약이 다음 아군 턴에 한꺼번에 회복 확인
            Assert.AreEqual(fullBefore + 6, full.CurrentHealth); // 최대 체력에서 신청해도 다음 부상을 회복 확인
            Assert.IsTrue(fallen.IsDead); // 전투불능 대상에게 예약 회복이나 부활 적용 방지
            Assert.IsNull(doubled.GetStatusEffect(BattleStatusEffectType.DelayedHealing)); // 다음 턴에 쓴 예약이 상태 목록에서 제거 확인
            Assert.IsNull(full.GetStatusEffect(BattleStatusEffectType.DelayedHealing)); // 다른 대상의 한 번 예약도 제거 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 아군 턴 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 그 다음 아군 턴으로 이동
            Assert.AreEqual(52, doubled.CurrentHealth); // 새 카드 사용이 없으면 예약의 반복 회복 차단
        } // 예약 대상별 검사 종료
    } // 생명수 검사 종료

    private static EnemyData CreateEnemy(bool undead, int resistance) // 실제 마법 저항과 불사형 분류가 다른 적 검사 원본
    { // 적 검사 원본 준비 시작
        var data = ScriptableObject.CreateInstance<EnemyData>(); // 별개 적 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"maxHealth\":200,\"isUndead\":{(undead ? "true" : "false")},\"magicalResistance\":{resistance}}}", data); // 실제 선택 분류와 저항 수치 지정
        return data; // 범위 타격을 검증할 적 원본 반환
    } // 적 검사 원본 준비 종료

    private static IEnumerator PlayAllyCard(Fight fight, string id, BattleUnitRuntime target) // 손패와 아군의 실제 카드 사용 흐름 호출
    { // 단일 아군 카드 사용 시작
        CardData source = fight.View(id).RuntimeCard.SourceData; // 실제 손패 보물 원본 조회
        return WinterfallCommonCardExpansionTests.Play(source, fight.Hand, FindView(fight, target), fight.Runner, fight.Deck, fight.Points); // 기존 카드 이동 및 행동력 검증과 실제 클릭 사용
    } // 단일 아군 카드 사용 종료

    private static BattleUnitView FindView(Fight fight, BattleUnitRuntime target) // 실제 런타임 아군의 선택 대상 화면 검색
    { // 대상 화면 검색 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == target); // 현재 전투 런타임과 일치하는 화면 반환
    } // 대상 화면 검색 종료
} // 성배 카드 검사 종료
