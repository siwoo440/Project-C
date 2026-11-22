using System; // 임시 유물 보유와 원래 회차 복원
using System.Collections; // 실제 전투 연출의 대기 동작
using NUnit.Framework; // 체력 손실과 실제 피해 출처 단언
using UnityEngine; // 실제 카드 데이터의 직렬화와 원화 조회
using UnityEngine.TestTools; // 유니티 실제 실행 모드 시험
using Fight = WinterfallSwordCombatTests.Fight; // 보유 카드와 실제 적 클릭 경로 사용
using Object = UnityEngine.Object; // 검사 전용 자산 제거

public sealed class BlackSwampLeechAndExecutionTests // 검은 늪 후반의 회복과 광역 처치 반응 확인
{ // 실제 유물 두 종류의 독립 전투 검사 시작
    [UnityTest] // 실제 단일 연타 및 광역 구별 확인
    public IEnumerator LeechUsesOnlyActualPrimaryHealthLossAndRoundsOnceBeforeVictory() // 실제 체력과 단일 카드의 회복 계산
    { // 전투 카드 회복 검사 시작
        yield return new EnterPlayMode(); // 기존 손패와 공격 연출 실제 구동
        using (var necklace = new OwnedRelic("LeechFangNecklace")) // 정식 거머리 이빨목걸이 장착
        { // 한 명 대상 연타와 막힌 피해 대조 시작
            var multi = MakeCard("LEECH_MULTI", CardTargetType.SingleEnemy, 12, 2); // 같은 적을 두 번 공격하는 실제 수치
            var area = MakeCard("LEECH_AREA", CardTargetType.AllEnemies, 32, 1); // 적이 한 명이어도 원래 범위 공격
            var lethal = MakeCard("LEECH_LETHAL", CardTargetType.SingleEnemy, 99, 1); // 실제 체력을 넘기는 높은 원래 피해
            try // 유니티 원본과 카드 정리 보장
            { // 실제 회복 검사 카드 실행 시작
                using (var fight = new Fight(new[] { multi, area, lethal })) // 같은 아군에게 배정되는 원래 유료 손패
                { // 첫 회차의 연타와 광역 처리 시작
                    var ally = fight.Allies[0]; // 실제 원래 카드 소유자
                    var target = fight.Enemies[0]; // 연타와 남은 적 대조의 같은 대상
                    ally.TakeUnblockedDamage(15, BattleDamageType.None, null); // 최대 체력 회복 상한과 분리할 초기 부상
                    int before = ally.CurrentHealth; // 회복 이전 생존 소유자 체력
                    int firstHealth = target.CurrentHealth; // 실제 한 대상의 원래 체력
                    target.GainBlock(4); // 첫 타격 일부를 막을 실제 방어도
                    int recoveryEvents = 0; // 유물 회복을 두 타격마다 따로 지급하는지 확인
                    ally.HealthRestored += (_, _) => recoveryEvents++; // 실제 유닛의 체력 회복 이벤트
                    yield return fight.Use("LEECH_MULTI", 0); // 같은 한 적에 체력 8과 12의 두 타격
                    Assert.AreEqual(20, firstHealth - target.CurrentHealth); // 방어도가 막은 네 점을 제외한 실제 합계
                    Assert.AreEqual(before + 1, ally.CurrentHealth); // 각 타격을 따로 내리면 잃는 한 점의 회복
                    Assert.AreEqual(1, recoveryEvents); // 한 카드에서 실제 체력 회복 한 번
                    for (int index = 1; index < fight.Enemies.Length; index++) // 원래 적의 나머지 세 칸 제거
                    { // 한 명만 남기는 광역 대조 설정 시작
                        var other = fight.Enemies[index]; // 제거할 별도 배치 적
                        other.TakeUnblockedDamage(other.CurrentHealth, BattleDamageType.None, null); // 유물과 무관한 인위적 정리
                    } // 한 명만 남기는 광역 대조 설정 종료
                    before = ally.CurrentHealth; // 광역 사용 전 실제 부상 체력
                    yield return fight.Use("LEECH_AREA"); // 현재 살아 있는 원래 하나의 적에 광역 명중
                    Assert.AreEqual(before, ally.CurrentHealth); // 실제 남은 적 수로 범위 카드가 단일 공격으로 바뀌지 않음
                    target.TakeUnblockedDamage(target.CurrentHealth - 5, BattleDamageType.None, null); // 높은 공격치와 실제 마지막 다섯 체력 대조
                    yield return fight.Use("LEECH_LETHAL", 0); // 마지막 적의 다섯 체력만 원래 직접 감소
                    Assert.AreEqual(before, ally.CurrentHealth); // 넘친 표시 피해가 유물 회복의 원본이 되지 않음
                } // 첫 회차의 연타와 광역 처리 종료
                using (var finalFight = new Fight(new[] { lethal })) // 전투 승리 전에 남은 체력 회복 저장 확인
                { // 마지막 적 승리 검사 시작
                    var ally = finalFight.Allies[0]; // 새 생존 소유자
                    var target = finalFight.Enemies[0]; // 마지막 타격 상대
                    ally.TakeUnblockedDamage(15, BattleDamageType.None, null); // 실제 승리 회복 전 초기 부상
                    int wounded = ally.CurrentHealth; // 승리 시점 비교용 체력
                    foreach (var enemy in finalFight.Enemies) // 최종 한 적과 다른 적의 분리
                    { // 실제 적별 타격 준비 시작
                        enemy.TakeUnblockedDamage(enemy == target ? enemy.CurrentHealth - 25 : enemy.CurrentHealth, BattleDamageType.None, null); // 마지막 적에게만 체력 25 유지
                    } // 실제 적별 타격 준비 종료
                    int victoryHealth = -1; // 실제 전투 종료 당시 체력 기록
                    finalFight.Turns.BattleCompleted += _ => victoryHealth = ally.CurrentHealth; // 승리 알림 전 유물 회복 순서 검증
                    yield return finalFight.Use("LEECH_LETHAL", 0); // 실제 마지막 적 체력 25만 깎는 카드
                    Assert.AreEqual(wounded + 1, victoryHealth); // 전투 결과를 기록하는 시점에 원래 소유자 체력 회복 반영
                } // 마지막 적 승리 검사 종료
            } // 실제 회복 검사 카드 실행 종료
            finally // 합성 원본의 종료 정리
            { // 검사 카드 정리 시작
                Object.Destroy(multi); // 임시 연타 카드 제거
                Object.Destroy(area); // 임시 광역 카드 제거
                Object.Destroy(lethal); // 임시 단일 카드 제거
            } // 검사 카드 정리 종료
        } // 한 명 대상 연타와 막힌 피해 대조 종료
        yield return new ExitPlayMode(); // 실제 유물 회차와 실행 모드 정리
    } // 전투 카드 회복 검사 종료

    [UnityTest] // 실제 전투불능 알림과 재진입 차단 확인
    public IEnumerator ExecutionMaskDealsNormalSixWithCreditAndDoesNotChain() // 활성 출혈 적 처치의 독립 광역 피해
    { // 원래 처치와 부가 피해 전투 확인 시작
        yield return new EnterPlayMode(); // 실제 게임 실행 모드 시작
        using (var mask = new OwnedRelic("ObsidianExecutionMask")) // 원본 흑요석 참수면 장착
        using (var fight = new Fight("DOUBLE_SLASH")) // 같은 전투 원래 손패와 적 네 칸 준비
        { // 실제 첫 출혈 처치와 추가 적을 검증
            var ally = fight.Allies[0]; // 첫 처치와 광역 피해의 원래 아군 소유자
            var first = fight.Enemies[0]; // 실제 출혈이 남은 원래 처치
            var second = fight.Enemies[1]; // 광역으로 쓰러질 두 번째 출혈 적
            var blocked = fight.Enemies[2]; // 일부 실제 방어도로 피해를 막을 생존 적
            var plain = fight.Enemies[3]; // 여섯 피해 전체를 받을 생존 적
            first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 3, 1, ally); // 첫 번째 처치 전 원래 활성 출혈
            second.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 3, 1, ally); // 광역으로 쓰러져도 추가 광역이 없어야 할 출혈
            second.TakeUnblockedDamage(second.CurrentHealth - 5, BattleDamageType.None, null); // 광역 여섯 피해보다 낮은 실제 체력
            blocked.GainBlock(2); // 원래 일반 피해가 소모할 두 점의 방어도
            int blockedBefore = blocked.CurrentHealth; // 방어가 있는 적의 전투 시작 체력
            int plainBefore = plain.CurrentHealth; // 일반 적의 전투 시작 체력
            BattleUnitRuntime credited = null; // 광역으로 죽은 원래 피해의 실제 아군 출처
            second.Defeated += (source, _) => credited = source; // 기존 처치 구독 계약으로 광역 출처 관측
            var pendingCard = fight.View("DOUBLE_SLASH").RuntimeCard; // 아직 소비하지 않은 원래 카드 피해 문맥
            try // 원래 카드 문맥 정리 보장
            { // 광역과 카드 문맥의 분리 검사 시작
                BattleCardDamageContext.Begin(pendingCard, new[] { plain }); // 외부 광역이 빼앗으면 안 될 다음 원래 카드 대상
                first.TakeUnblockedDamage(first.CurrentHealth, BattleDamageType.None, ally); // 실제 먼저 출혈 상태 적에게 귀속된 처치
                Assert.IsTrue(BattleCardDamageContext.TryConsume(out var preservedCard, out var target)); // 광역 피해 뒤에도 남은 원래 약점 문맥
                Assert.AreSame(pendingCard, preservedCard); // 기존 실제 카드 원본 문맥 유지
                Assert.AreSame(plain, target); // 다음 원래 적 대상 인덱스 유지
            } // 광역과 카드 문맥의 분리 검사 종료
            finally // 차기 검사 등에 이전 피해 문맥 남김 방지
            { // 원래 카드 약점 문맥 정리 시작
                BattleCardDamageContext.Clear(); // 공유 기존 직접 타격 문맥 초기화
            } // 원래 카드 약점 문맥 정리 종료
            Assert.IsTrue(second.IsDead); // 광역 정상 피해로 적 두 번째 실제 처치
            Assert.AreSame(ally, credited); // 원래 아군의 실제 처치 귀속을 후속 사건에도 보존
            Assert.AreEqual(blockedBefore - 4, blocked.CurrentHealth); // 여섯 일반 피해에 실제 방어도 두 점 소모
            Assert.AreEqual(0, blocked.CurrentBlock); // 원래 일반 피해 경로의 실제 방어 차감
            Assert.AreEqual(plainBefore - 6, plain.CurrentHealth); // 재귀 광역이 생기지 않은 생존 적의 한 번 피해
        } // 실제 첫 출혈 처치와 추가 적을 검증 종료
        using (var mask = new OwnedRelic("ObsidianExecutionMask")) // 독립 대조 교전에 같은 원본 유물 장착
        using (var fight = new Fight("DOUBLE_SLASH")) // 별개의 처치 귀속과 출혈 대조
        { // 원래 제외 조건의 실제 피해 처리 시작
            var ally = fight.Allies[0]; // 원래 유효 아군 출처
            var last = fight.Enemies[3]; // 피해를 관측할 변하지 않는 마지막 적
            int before = last.CurrentHealth; // 아무 광역도 적용되지 않은 원본 체력
            fight.Enemies[0].TakeUnblockedDamage(fight.Enemies[0].CurrentHealth, BattleDamageType.None, ally); // 출혈 없는 적의 아군 귀속 처치
            fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, ally); // 다음 대조 적에 정상 출혈 부여
            fight.Enemies[1].TakeUnblockedDamage(fight.Enemies[1].CurrentHealth, BattleDamageType.None, null); // 출처가 없는 활성 출혈 적 처치
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, ally); // 마지막 대조 적에 정상 출혈 여부
            fight.Enemies[2].TakeUnblockedDamage(fight.Enemies[2].CurrentHealth, BattleDamageType.None, last); // 적이 귀속된 같은 진영 처치
            Assert.AreEqual(before, last.CurrentHealth); // 제외 조건 세 종류 모두 광역 미발동
        } // 원래 제외 조건의 실제 피해 처리 종료
        yield return new ExitPlayMode(); // 실제 게임 실행 모드 정리
    } // 원래 처치와 부가 피해 전투 확인 종료

    private static CardData MakeCard(string id, CardTargetType target, int damage, int hits) // 실제 명중에 필요한 최소 카드 데이터 구성
    { // 검사 전용 카드 자료 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // Unity의 실제 카드 자료 원본
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_{id}\",\"displayName\":\"거머리 검사\",\"cardType\":{(int)CardType.Sword},\"targetType\":{(int)target},\"effectType\":{(int)CardEffectType.Damage},\"apCost\":1,\"damageType\":{(int)BattleDamageType.Physical},\"effectValue\":{damage},\"damageHitCount\":{hits}}}", card); // 고정 피해와 원래 범위의 두 카드 집합
        return card; // 실제 전투에 공급할 합성 원본
    } // 검사 전용 카드 자료 생성 종료

    private sealed class OwnedRelic : IDisposable // 원본 유물의 회차 소유 확인
    { // 실제 보유 회차의 원복 보장 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 검사 시작 전 기존 회차 확인
        private readonly RelicRunManager manager; // 실제 게임의 보유 유물 관리자
        private readonly RelicData relic; // 원래 원화와 효과를 포함한 유물 에셋

        public OwnedRelic(string key) // 지정된 실제 유물의 보유 준비
        { // 유물 원본 획득 시작
            relic = Resources.Load<RelicData>($"Relics/{key}"); // 같은 게임이 실제 쓰는 원본 유물 로드
            Assert.IsNotNull(relic); // 유효한 원본이 있어야 검사 가능
            manager = RelicRunManager.EnsureInstance(); // 현재 회차 관리자를 실제 경로로 확보
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 회차 소유 목록에 등록
        } // 유물 원본 획득 종료

        public void Dispose() // 원래 회차와 검사 소유 복원
        { // 테스트 장착 해제 시작
            manager.TryRemove(relic.RelicId); // 이번에 장착한 원본만 회차 목록에서 제거
            if (previous == null) // 최초 검사에서 직접 만든 경우만 환경 정리
            { // 테스트 관리자의 원래 유니티 제거 시작
                Object.Destroy(manager.gameObject); // 새로 만든 임시 회차 객체 제거
            } // 테스트 관리자의 원래 유니티 제거 종료
        } // 테스트 장착 해제 종료
    } // 실제 보유 회차의 원복 보장 종료
} // 실제 유물 두 종류의 독립 전투 검사 종료
