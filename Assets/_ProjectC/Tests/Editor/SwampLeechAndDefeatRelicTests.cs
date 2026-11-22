using System; // 실제 유물 장착과 검사 뒤 상태 정리
using System.Collections; // 카드 연출 및 실제 유니티 검사 대기
using NUnit.Framework; // 출혈과 중독 및 체력 상태의 실제 수치 비교
using UnityEngine; // 유물 원본과 네이티브 난수 시드 준비
using UnityEngine.TestTools; // 실제 게임에서 실행하는 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 완료한 손패 결제와 네이티브 전투 재사용
using Object = UnityEngine.Object; // 합성 원본과 유물 관리자 정리
using Random = UnityEngine.Random; // 실제 게임 효과에서 쓰는 원래 난수

public sealed class SwampLeechAndDefeatRelicTests // 늪 거머리 및 사망 상태 연계의 실제 전투 검사
{ // 실제 유물 세 종류의 독립 검사 시작
    [UnityTest] // 진짜 카드가 여러 적을 때릴 때 입힌 체력 손실 계산
    public IEnumerator SwampLeechAggregatesOnlyPreHitBleedingBodiesAndHealsBeforeVictory() // 출혈 적들에 실제로 준 피해를 합산해 한 번 회복
    { // 광역 피해와 마지막 생존 적의 처치 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 목표 확인에서 원래 카드 실행
        using (var relic = new OwnedRelic("SwampLeechSinew")) // 원본 늪지 거머리 심줄 장착
        { // 네이티브 광역 효과와 승리 이벤트 대조 시작
            var card = MakeAreaCard(); // 실제 피해 카드의 광역 대상과 원본 피해 고정
            try // 플레이 모드에서 만든 카드 원본 정리 보장
            { // 같은 광역 카드가 서로 다른 출혈 대상에 들어가는 상태
                using (var fight = new Fight(new[] { card })) // 네이티브 실제 수치의 기본 네 적 배치
                { // 같은 한 번의 유료 원본에서 대상마다 실제 확인
                    var actor = fight.Allies[0]; // 실제 피해 원본을 쓴 살아 있는 아군
                    actor.TrySpendHealth(15); // 나중에 회복 받을 수 있는 실제 체력 부족
                    int wound = actor.CurrentHealth; // 카드 정산 전 출발 체력
                    var lethal = fight.Enemies[0]; // 체력 일부만 남은 첫 번째 출혈 적
                    var guarded = fight.Enemies[1]; // 실제 방어도에 남은 두 번째 출혈 적
                    lethal.TakeUnblockedDamage(lethal.CurrentHealth - 6, BattleDamageType.None, null); // 표시 원본 피해보다 작은 실제 체력만 유지
                    lethal.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, actor); // 첫 실제 원본 타격 전에 존재하는 출혈
                    guarded.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, actor); // 방어도로 일부분 막을 두 번째 출혈
                    guarded.GainBlock(13); // 일반 공격 18의 13을 막는 실제 보호
                    int guardedHealth = guarded.CurrentHealth; // 방어 뒤 실제 감소할 이전 체력
                    int heals = 0; // 현재 한 카드의 실제 회복 횟수
                    actor.HealthRestored += (_, _) => heals++; // 네이티브 체력 회복 이벤트
                    yield return fight.Use("SWAMP_LEECH_AREA"); // 유료 광역 카드 한 장의 네이티브 화면 동작
                    Assert.IsTrue(lethal.IsDead); // 상태가 정리되는 실제 마지막 체력 손실 포함
                    Assert.AreEqual(5, guardedHealth - guarded.CurrentHealth); // 방어도로 막힌 값과 실제 감소 구분
                    Assert.AreEqual(wound + 1, actor.CurrentHealth); // 출혈 적 체력 손실 여섯과 다섯의 합으로 한 번 내림
                    Assert.AreEqual(1, heals); // 여러 명을 때려도 실제 회복은 카드를 기준으로 한 번
                } // 같은 한 번의 유료 원본의 대상 확인 종료
                using (var fight = new Fight(new[] { card })) // 같은 카드로 실제 마지막 생존자의 승리 정산 검증
                { // 마지막 체력 손실의 실제 회복 시점 확인
                    var actor = fight.Allies[0]; // 실제 유료 카드 소유자
                    actor.TrySpendHealth(15); // 최종 회복이 실제 체력에 들어갈 초기 부상
                    int wound = actor.CurrentHealth; // 실제 승리 전에 비교할 체력
                    var last = fight.Enemies[0]; // 최종 전투불능이 될 적
                    foreach (var enemy in fight.Enemies) // 이외 적의 원래 상태 정리
                    { // 원래 단일 생존 적의 대조 자료
                        enemy.TakeUnblockedDamage(enemy == last ? enemy.CurrentHealth - 11 : enemy.CurrentHealth, BattleDamageType.None, null); // 마지막 적에게 실제 열한 체력만 유지
                    } // 원래 단일 생존 적의 대조 자료 정리
                    last.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, actor); // 마지막 실제 순간의 출혈 여부
                    int savedHealth = -1; // 전투 완료 때 보일 소유자 체력
                    fight.Turns.BattleCompleted += _ => savedHealth = actor.CurrentHealth; // 기존 전투 완료 알림 시점의 실제 저장 대상
                    yield return fight.Use("SWAMP_LEECH_AREA"); // 최종 적을 실제 원래 광역 공격으로 처치
                    Assert.AreEqual(wound + 1, savedHealth); // 원래 마지막 승리 정산 전에 적용하는 한 점 회복
                } // 마지막 체력 손실의 실제 회복 시점 확인 종료
            } // 같은 광역 카드의 여러 적 연계 검사 종료
            finally // 실제 임시 원본의 정리 보장
            { // 기존 검사 코드에서만 만든 카드 제거 시작
                Object.Destroy(card); // 테스트가 소유하는 합성 광역 카드 파일 제거
            } // 기존 검사 코드에서만 만든 카드 제거 종료
        } // 네이티브 광역 효과와 승리 이벤트 대조 종료
        yield return new ExitPlayMode(); // 독립 테스트 플레이 모드 종료
    } // 광역 피해와 마지막 생존 적의 처치 검사 종료

    [UnityTest] // 실제 사망 때 중독을 보관하고 한 번만 난수 판정
    public IEnumerator VenomUsesOneRollAndChecksPoisonBeforeDeathStatusPurge() // 무귀속 중독 적과 참수면의 중첩 사망의 정상 중독
    { // 중독 상태와 고정 시드로 실제 사망 반응 확인 시작
        yield return new EnterPlayMode(); // 실제 적 상태와 원래 사망 이벤트 준비
        using (var relic = new OwnedRelic("VenomSapCore")) // 실제 독수액 핵 보유
        { // 실제 출처 없는 죽음과 참수면 중첩 비교 시작
            using (var fight = new Fight("DOUBLE_SLASH")) // 기본 전투 원본과 실제 생존 적 네 칸
            { // 25퍼센트 경계와 약화 면역의 실제 검사
                var missed = fight.Enemies[0]; // 첫 번째 무귀속 중독 사망 대상
                var proc = fight.Enemies[1]; // 두 번째 실제 부여 성공의 중독 대상
                var receiver = fight.Enemies[2]; // 생존 적에게 부여할 원래 두 턴의 중독
                var immune = fight.Enemies[3]; // 같은 광역 적용에서 면역 확인 대상
                missed.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 실제 죽기 전에 존재한 출처 없는 중독
                proc.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 다음 사망의 출처 없는 활성 중독
                immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 기존 약화 처리기의 실제 면역
                Random.InitState(FindSeed(25)); // 원래 25퍼센트 확률 바로 밖의 첫 난수
                missed.TakeUnblockedDamage(missed.CurrentHealth, BattleDamageType.None, null); // 실제 사망 알림에 귀속 없는 첫 대상
                Assert.IsNull(receiver.GetStatusEffect(BattleStatusEffectType.Poison)); // 확률 실패에서는 실제 새 상태 없음
                int seed = FindSeed(24); // 성공 가능한 마지막 경계의 시드
                Random.InitState(seed); // 현재 독수액 적용 이전의 값으로 리셋
                Random.Range(0, 100); // 같은 한 번의 확률 소모를 수동 비교
                int nextRoll = Random.Range(0, 1000000); // 유물 뒤 원래 남아 있어야 할 다음 난수
                Random.InitState(seed); // 원본 사망의 실제 난수 입력 복원
                proc.TakeUnblockedDamage(proc.CurrentHealth, BattleDamageType.None, null); // 원래 사망 상태 삭제 전 출처 없는 실제 중독
                Assert.AreEqual(nextRoll, Random.Range(0, 1000000)); // 생존 적별 재추첨 없이 실제 사망에서 한 번만 난수
                var poison = receiver.GetStatusEffect(BattleStatusEffectType.Poison); // 실제 일반 중독 상태 조회
                Assert.AreEqual(1, poison?.EffectiveValue); // 원본 독수액 핵의 실제 중독량
                Assert.AreEqual(2, poison?.RemainingTurns); // 두 실제 적 턴 동안 남는 상태
                Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 적의 면역과 일반 상태 적용 보존
                immune.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 이제 같은 전투에서 일반 중독만 받을 대조 적
                immune.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 독수액 출처가 없는 실제 한 점의 일반 중독
                int healthBeforePoison = receiver.CurrentHealth; // 독수액 실제 첫 적 차례 전 체력
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 기존 정규 흐름으로 첫 실제 적 차례 시작
                Assert.AreEqual(healthBeforePoison - 1, receiver.CurrentHealth); // 독수액 중독의 첫 번째 실제 발동
                Assert.AreEqual(1, receiver.GetStatusEffect(BattleStatusEffectType.Poison)?.EffectiveValue); // 다음 적 차례까지 중독량 한 점 보존
                Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 일반 중독은 첫 감소 후 정상 소진
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn() && fight.Turns.EndPlayerTurn()); // 실제 첫 적 차례 종료와 두 번째 시작
                Assert.AreEqual(healthBeforePoison - 2, receiver.CurrentHealth); // 두 번째 차례의 한 점 피해 적용
                Assert.IsNull(receiver.GetStatusEffect(BattleStatusEffectType.Poison)); // 정확히 두 번 실제 발동 뒤의 정상 중독 제거
            } // 25퍼센트 경계와 약화 면역의 실제 검사 종료
            using (var mask = new OwnedRelic("ObsidianExecutionMask")) // 실제 참수면의 중첩 사망에도 별개 독수액 반응
            using (var fight = new Fight("DOUBLE_SLASH")) // 실제 네이티브 참수면 연계 전투
            { // 중첩 사망을 원래 상태로 검사 시작
                var actor = fight.Allies[0]; // 참수면 원래 사망의 실제 아군 소유자
                var first = fight.Enemies[0]; // 참수면을 유발할 원래 출혈 적
                var nested = fight.Enemies[1]; // 참수면의 고정 피해로 쓰러지는 중독 적
                var last = fight.Enemies[2]; // 중첩 사망에서 중독이 확산될 실제 적
                first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, actor); // 원래 참수면용 실제 출혈 상태
                nested.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 중첩 사망 당시 먼저 존재한 실제 중독
                nested.TakeUnblockedDamage(nested.CurrentHealth - 5, BattleDamageType.None, null); // 참수면 여섯보다 적은 실제 체력을 설정
                Random.InitState(FindSeed(24)); // 중첩 사망에서 원래 독수액 난수 성공
                first.TakeUnblockedDamage(first.CurrentHealth, BattleDamageType.None, actor); // 체력이 영이 되는 실제 아군 첫 처치
                Assert.IsTrue(nested.IsDead); // 네이티브 참수면이 두 번째 실제 전투불능 원인
                Assert.AreEqual(1, last.GetStatusEffect(BattleStatusEffectType.Poison)?.EffectiveValue); // 참수면 재발동 차단 중에도 실제 독수액 반응 유지
            } // 중첩 사망의 원래 상태 검사 종료
        } // 실제 출처 없는 죽음과 참수면 중첩 비교 종료
        yield return new ExitPlayMode(); // 실제 Unity 검사 모드 정리
    } // 중독 상태와 고정 시드의 실제 사망 반응 확인 종료

    [UnityTest] // 원래 체력 지불과 실제 원인 있는 처치 회복을 분리
    public IEnumerator PredatorOpeningChargesEveryLivingAllyAndOnlyHealsTheCreditedKiller() // 각 아군의 일반 체력 비용 및 원래 처치 귀속
    { // 체력 비용과 회복의 전투 검사 시작
        yield return new EnterPlayMode(); // 기존 전투 초기화와 사망 경계 사용
        using (var relic = new OwnedRelic("HollowPredatorStomach")) // 포식괴물의 빈 위장 원본 보유
        using (var fight = new Fight("DOUBLE_SLASH")) // 실제 전투 시작과 아군 세 명
        { // 현재 전투의 개별 비용과 회복 검사 시작
            foreach (var ally in fight.Allies) // 실제 생존 파티의 모든 소유자 한 번씩 확인
            { // 시작 체력 비용 검사
                Assert.AreEqual(ally.MaxHealth - 5, ally.CurrentHealth); // 원래 최대 체력을 바꾸지 않는 실제 초기 비용 다섯
            } // 시작 체력 비용 검사 종료
            var killer = fight.Allies[1]; // 두 번째 아군이 마지막 체력 피해를 준 상태
            var other = fight.Allies[0]; // 전투 이후 회복되지 않아야 할 다른 출전 아군
            killer.TrySpendHealth(8); // 원래 회복 열을 온전히 적용할 추가 부상
            int beforeKiller = killer.CurrentHealth; // 실제 회복 적용 전 본인 체력
            int beforeOther = other.CurrentHealth; // 원래 회복 대상이 아닌 아군의 체력
            var noCredit = fight.Enemies[0]; // 실제 사망해도 아군 출처가 없는 대조
            var credited = fight.Enemies[1]; // 다음 실제 아군의 적 처치 대조
            noCredit.TakeUnblockedDamage(noCredit.CurrentHealth, BattleDamageType.None, null); // 귀속이 없는 적 전투불능
            Assert.AreEqual(beforeKiller, killer.CurrentHealth); // 소유자 없는 전투불능의 회복 제외 확인
            credited.TakeUnblockedDamage(credited.CurrentHealth, BattleDamageType.None, killer); // 다른 소유자가 처치한 실제 적
            Assert.AreEqual(beforeKiller + 10, killer.CurrentHealth); // 원래 처치한 실제 살아 있는 아군의 체력 회복
            Assert.AreEqual(beforeOther, other.CurrentHealth); // 관련 없는 다른 생존 아군의 현재 체력 유지
        } // 현재 전투의 개별 비용과 회복 검사 종료
        yield return new ExitPlayMode(); // 실제 검사 전투 정리
    } // 체력 비용과 회복의 전투 검사 종료

    private static CardData MakeAreaCard() // 네이티브 광역 공격으로 쓸 원본 구성
    { // 실제 테스트 손패 원본 준비 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 유니티가 직접 읽는 카드 원본
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_SWAMP_LEECH_AREA\",\"displayName\":\"늪지 출혈 검사\",\"cardType\":{(int)CardType.Sword},\"targetType\":{(int)CardTargetType.AllEnemies},\"effectType\":{(int)CardEffectType.Damage},\"apCost\":1,\"damageType\":{(int)BattleDamageType.Physical},\"effectValue\":18}}", card); // 네이티브 광역과 물리 피해의 고정 설정
        return card; // 기존 실제 싸움에서 뽑을 합성 카드
    } // 실제 테스트 손패 원본 준비 종료

    private static int FindSeed(int percentile) // 실제 무작위 판정의 정확한 경계 입력 찾기
    { // 기존 Unity 시드 탐색과 상태 복원 시작
        var original = Random.state; // 다른 실제 전투가 사용하던 난수 상태
        try // 검사 중 변경한 난수 보호
        { // 목표 백분율의 정확한 첫 입력 찾기
            for (int seed = 0; seed < 10000; seed++) // 실제 가능한 시드를 제한된 범위에서 탐색
            { // 같은 원래 확률 구간의 진짜 난수 확인 시작
                Random.InitState(seed); // 이번에 확인할 첫 실제 값
                if (Random.Range(0, 100) == percentile) // 요청한 백분율 경계와 일치하는지 확인
                { // 실제 시드를 찾은 경우 반환 시작
                    return seed; // 직접 실제 사망 직전에 사용할 결정적 값
                } // 실제 시드를 찾은 경우 반환 종료
            } // 같은 원래 확률 구간의 진짜 난수 확인 종료
            throw new InvalidOperationException("중독 검사 확률을 가진 실제 시드 없음"); // 정해진 실제 범위에서 찾지 못한 검사 실패
        } // 목표 백분율의 정확한 첫 입력 찾기 종료
        finally // 실제 검사 후 이전 난수로 돌리는 작업
        { // 이전 전투의 난수 복귀 시작
            Random.state = original; // 이 기능에서 유물의 실제 다음 난수 영역 보존
        } // 이전 전투의 난수 복귀 종료
    } // 기존 Unity 시드 탐색과 상태 복원 종료

    private sealed class OwnedRelic : IDisposable // 원래 유물 리소스의 임시 회차 보유 검사
    { // 실제 회차 유물 장착 해제 보장 시작
        private readonly RelicRunManager prior = RelicRunManager.Instance; // 독립 진입 전 실제 관리자
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 런 인벤토리 관리자 확보
        private readonly RelicData asset; // 지금 가진 원본 리소스 에셋
        public OwnedRelic(string name) // 이번 실제 유물의 보유 기록
        { // 원래 자원 및 회차 보유 획득 시작
            asset = Resources.Load<RelicData>("Relics/" + name); // 본 개발에서 연결한 실제 데이터를 조회
            Assert.IsNotNull(asset); // 실제 리소스 등록의 존재 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(asset)); // 이전 일반 보유와 같은 유물 지급
        } // 원래 자원 및 회차 보유 획득 종료
        public void Dispose() // 해당 검사에서 잠시 장착한 자료 정리
        { // 실제 런의 기존 보유를 유지하는 제거 시작
            manager.TryRemove(asset.RelicId); // 현재 시험에서 추가한 원본만 해제
            if (prior == null) // 기존 회차 관리자가 없었던 경우 확인
            { // 빈 상태로 되돌릴 임시 관리자 제거 시작
                Object.Destroy(manager.gameObject); // 이번 검사에서 생긴 Unity 관리자만 제거
            } // 빈 상태로 되돌릴 임시 관리자 제거 종료
        } // 실제 런의 기존 보유를 유지하는 제거 종료
    } // 실제 회차 유물 장착 해제 보장 종료
} // 실제 유물 세 종류의 독립 검사 종료
