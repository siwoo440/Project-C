using System; // 실제 독립 검사의 임시 자산 정리
using System.Collections; // 실제 카드 연출과 Unity 실행 대기
using System.Collections.Generic; // 실제 여러 타격의 수치 순서 기록
using NUnit.Framework; // 실제 수치와 상태 조건 비교
using UnityEngine; // 실제 유물 원본과 임시 보유 관리자 사용
using UnityEngine.TestTools; // Unity 실제 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 손패와 적 선택 검사 경로
using Object = UnityEngine.Object; // 이번 검사만 만든 회차 보유 게임 객체 정리

public sealed class WinterfallReactorAndGlovesTests // 원자로와 재 묻은 장갑의 직접 카드 피해 검사
{ // 두 고철 공장 유물의 피해 검사 시작
    [UnityTest] // 일치하는 각각의 적 상태와 원래 별개 피해 경로 확인
    public IEnumerator ReactorUsesOnlyOneFifteenPercentBonusAndSkipsAmbientOrEchoDamage() // 화상 및 감전 둘이 있어도 하나의 직접 피해 증가
    { // 기존 전투 피해와 새 유물 원본 비교 시작
        yield return new EnterPlayMode(); // 실제 유니티 전투 코드 사용
        using (var reactor = new OwnedRelic("ReactorHeartShard")) // 원자로 심장편의 실제 회차 획득
        using (var fight = new Fight("RUNE_BURST", "RUNE_BURST")) // 실제 직접 마법 원본과 비교 예정 카드
        { // 실제 적 상태 넷과 독립 피해 검증
            var ally = fight.Allies[0]; // 공격의 실제 출전 아군
            var ordinary = fight.Enemies[0]; // 두 활성 상태가 없는 적
            var burning = fight.Enemies[1]; // 화상만 실제 남은 적
            var shocked = fight.Enemies[2]; // 감전만 실제 남은 적
            var both = fight.Enemies[3]; // 화상과 감전이 함께 활성인 적
            burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1, ally); // 원래 실제 피해를 줄 수 있는 활성 화상
            shocked.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 3, 1, ally); // 같은 효과를 발동할 활성 감전
            both.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1, ally); // 두 실제 상태의 첫 조건
            both.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 3, 1, ally); // 두 실제 상태의 둘째 조건
            Assert.AreEqual(100, Preview(ordinary, ally, CardType.Wand)); // 기본 원본의 독립 피해 유지
            Assert.AreEqual(115, Preview(burning, ally, CardType.Wand)); // 화상만으로 방어 전 15퍼센트 증가
            Assert.AreEqual(115, Preview(shocked, ally, CardType.Wand)); // 감전만으로 같은 15퍼센트 증가
            Assert.AreEqual(115, Preview(both, ally, CardType.Wand)); // 두 활성 상태에서도 정확히 한 번
            Assert.AreEqual(115, Preview(both, ally, CardType.Wand)); // 상태나 발동 기회를 소모하지 않는 반복 예고
            Assert.AreEqual(3, both.GetStatusEffect(BattleStatusEffectType.Burn)?.RemainingTurns); // 반복 예고의 실제 화상 지속 보존
            Assert.AreEqual(3, both.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 반복 예고의 실제 감전 지속 보존
            var expired = shocked.GetStatusEffect(BattleStatusEffectType.Shock); // 만료해 볼 실제 감전 상태
            expired.AdvanceDuration(); // 원래 유효 시간 한 단계 감소
            expired.AdvanceDuration(); // 원래 유효 시간 두 단계 감소
            expired.AdvanceDuration(); // 목록에 남은 실제 만료 상태
            Assert.AreEqual(100, Preview(shocked, ally, CardType.Wand)); // 남아 있어도 만료된 상태의 증가 제외
            var outcome = default(BattleDamageResult); // 실제 유물의 직접 마법 피해 확인
            both.DamageTaken += (_, result) => outcome = result; // 실제 카드 타격의 원본 수치 관찰
            yield return fight.Use("RUNE_BURST", 3); // 두 상태 적에 대한 실제 마법 카드 한 번
            Assert.AreEqual(12, outcome.RawDamage); // 기본 열하나에 한 번 비율을 내림한 실제 피해
            Assert.AreEqual(20, both.TakeWandEchoDamage(20, ally).RawDamage); // 기존 독립 마법 반향에서 원래 피해 유지
            var queued = fight.View("RUNE_BURST").RuntimeCard; // 피해 출처가 아닌 주변 카드 문맥의 독립 대조
            BattleCardDamageContext.Begin(queued, new[] { burning }); // 남은 원래 카드의 문맥만 준비
            try // 일반 피해 경로가 문맥 우연으로 새 공격 보너스를 얻지 않는지 확인
            { // 주변 원래 카드 문맥을 가진 일반 피해 검사 시작
                Assert.AreEqual(20, burning.TakeDamage(20, BattleDamageType.None, ally).RawDamage); // 직접 맞힌 카드가 아닌 일반 독립 피해
            } // 주변 원래 카드 문맥을 가진 일반 피해 검사 종료
            finally // 다른 검사나 전투 카드에 남지 않을 문맥 초기화
            { // 실제 독립 카드 문맥 정리 시작
                BattleCardDamageContext.Clear(); // 지정된 가짜 대조의 원래 문맥 제거
            } // 실제 독립 카드 문맥 정리 종료
        } // 실제 적 상태 넷과 독립 피해 검증 종료
        yield return new ExitPlayMode(); // 이전 검사 환경으로 복구
    } // 기존 전투 피해와 새 유물 원본 비교 종료

    [UnityTest] // 실제 검의 여러 타격과 보조 타격에 대한 방어 전 고정 피해 확인
    public IEnumerator GlovesGrantTwoToEveryLandedSwordHitIncludingKingsFollowUp() // 화상 검 기본 명중과 검의 고유 후속 타격
    { // 원본 검 타격 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 연출 및 카드 입력 환경
        using (var gloves = new OwnedRelic("AshStainedGloves")) // 재 묻은 장갑의 실제 획득
        using (var fight = new Fight("KINGS_SLASH", "DOUBLE_SLASH", "DOUBLE_SLASH")) // 실제 전체 왕의 참격과 연속 검 두 장
        { // 실제 검 타격별 피해 비교 시작
            fight.Controller.CardUsed += (card, targets) => BattleCardDamageContext.Begin(card, targets); // 실제 전투 씬이 설정하는 원래 카드 및 대상 문맥 연결
            var ally = fight.Allies[0]; // 원래 검의 실제 소유 아군
            var burning = fight.Enemies[0]; // 왕의 참격 추가 타격을 받는 첫 번째 동률 적
            var shocked = fight.Enemies[1]; // 화상이 없는 감전 적
            var alsoBurning = fight.Enemies[2]; // 광역의 독립 다른 화상 적
            burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1, ally); // 첫 번째 적의 실제 화상 상태
            shocked.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 3, 1, ally); // 검 장갑의 제외 조건으로 사용할 감전
            alsoBurning.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1, ally); // 광역 독립 대상의 동일 화상 조건
            Assert.AreEqual(102, Preview(burning, ally, CardType.Sword)); // 실제 검 계열과 화상에서만 보이는 두 점
            Assert.AreEqual(100, Preview(burning, ally, CardType.Wand)); // 같은 화상에 대한 지팡이 피해 유지
            Assert.AreEqual(100, Preview(shocked, ally, CardType.Sword)); // 같은 검의 감전만 있는 대상 피해 유지
            var mainHits = new List<int>(); // 실제 첫 대상의 광역 및 후속 검 피해
            var otherHits = new List<int>(); // 다른 감전 대상의 실제 원본 피해
            var fireHits = new List<int>(); // 별도의 다른 화상 대상의 실제 검 피해
            burning.DamageTaken += (_, result) => mainHits.Add(result.RawDamage); // 원본 첫 적의 모든 실제 타격
            shocked.DamageTaken += (_, result) => otherHits.Add(result.RawDamage); // 독립 감전 적의 원래 타격
            alsoBurning.DamageTaken += (_, result) => fireHits.Add(result.RawDamage); // 광역의 다른 화상 적 원본
            yield return fight.Use("KINGS_SLASH"); // 실제 광역 검 공격과 고유 후속 공격 발동
            CollectionAssert.AreEqual(new[] { 20, 14 }, mainHits); // 기본 열여덟과 추가 열둘 모두 장갑 두 점 증가
            CollectionAssert.AreEqual(new[] { 18 }, otherHits); // 화상이 없는 적의 원래 광역 수치 유지
            CollectionAssert.AreEqual(new[] { 20 }, fireHits); // 다른 화상 적도 정상 광역 명중 하나에 장갑 적용
            yield return fight.Use("DOUBLE_SLASH", 0); // 이미 화상인 첫 대상에 실제 두 번 검 공격
            CollectionAssert.AreEqual(new[] { 20, 14, 6, 6 }, mainHits); // 같은 카드의 두 독립 명중마다 각각 두 점 가산
            ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 2, 1); // 기존 실제 명중 판정으로 확정 빗나감 준비
            yield return fight.Use("DOUBLE_SLASH", 0); // 실제 유료 검 공격은 두 타격 모두 빗나감
            CollectionAssert.AreEqual(new[] { 20, 14, 6, 6 }, mainHits); // 빗나간 독립 공격에는 새 피해 이벤트 없음
        } // 실제 검 타격별 피해 비교 종료
        yield return new ExitPlayMode(); // 같은 Unity 원래 환경 복구
    } // 원본 검 타격 검사 종료

    [UnityTest] // 이전 두 비율과 새로운 두 유물의 수치 순서 보존
    public IEnumerator EarlierPincerAndBadgeResolveBeforeReactorAndFlatGloves() // 방어 이전 기존 비율 및 새 고정 가산의 실제 순서 검사
    { // 공용 피해 예고의 실제 네 보유 효과 시작
        yield return new EnterPlayMode(); // 실제 보유 회차의 테스트 실행
        using (var pincer = new OwnedRelic("CompressionPincerHandle")) // 앞선 행동력 약화 비율 유물
        using (var badge = new OwnedRelic("SecurityEnforcerBadge")) // 앞선 기절과 행동력 약화 비율 유물
        using (var reactor = new OwnedRelic("ReactorHeartShard")) // 새 단일 15퍼센트 두 상태 조건
        using (var gloves = new OwnedRelic("AshStainedGloves")) // 새 검 직접 명중의 고정 두 점
        using (var fight = new Fight("RUNE_BURST")) // 실제 방어 전 피해 계산이 연결된 교전
        { // 현재 실제 적의 겹치는 활성 상태 설정
            var ally = fight.Allies[0]; // 실제 피해 보정의 아군 출처
            var enemy = fight.Enemies[0]; // 여러 약화가 동시에 있는 생존 적
            enemy.ApplyStatusEffect(BattleStatusEffectType.ActionPointDown, 1, 1, 1, ally); // 앞선 행동력 감소가 실제 유효한 상태
            enemy.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1, ally); // 새 심장편과 장갑의 실제 화상 조건
            enemy.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 3, 1, ally); // 새 심장편에 추가 발동시키지 않을 감전 조건
            Assert.AreEqual(146, Preview(enemy, ally, CardType.Sword)); // 백에서 집게 백십, 배지 백이십육, 심장편 백사십사, 장갑 두 점
            Assert.AreEqual(144, Preview(enemy, ally, CardType.Wand)); // 같은 비율에서 지팡이는 장갑 고정 두 점 제외
        } // 현재 실제 적의 겹치는 활성 상태 설정 종료
        yield return new ExitPlayMode(); // 독립 유니티 테스트 종료
    } // 공용 피해 예고의 실제 네 보유 효과 종료

    private static int Preview(BattleUnitRuntime enemy, BattleUnitRuntime ally, CardType type) // 실제 공용 피해 예고에서 방어 전 원본 값을 읽는 검사
    { // 원래 방어 전 수치 결과를 확인
        return enemy.PreviewDamage(100, BattleDamageType.Physical, ally, alliedDirectAttack: true, directCardType: type).RawDamage; // 원본 검 및 지팡이 구분을 포함한 같은 피해 계산
    } // 원래 방어 전 수치 결과 확인 종료

    private sealed class OwnedRelic : IDisposable // 독립 전투에 실제 유물 하나씩을 장착하고 해제
    { // 별도 보유 원본 등록 준비
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 이 검사 이전의 원래 보유 객체
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 유물의 현재 정상 인벤토리
        private readonly RelicData relic; // 장착한 실제 게임 원본

        public OwnedRelic(string stem) // Resources의 실제 유물 에셋으로 보유 설정
        { // 검증용 실제 유물 로드 시작
            relic = Resources.Load<RelicData>("Relics/" + stem); // 상점과 도감에서 함께 쓰는 원본 유물
            Assert.IsNotNull(relic, stem); // 실제 에셋 준비 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 원래 게임의 실제 유물 획득 결과
        } // 검증용 실제 유물 로드 종료

        public void Dispose() // 이 검사에서 얻은 원래 유물만 정리
        { // 기존 회차 상태 복원 시작
            manager.TryRemove(relic.RelicId); // 이번 보유로 추가된 해당 실제 원본 제거
            if (previous == null) // 테스트에서 처음 만든 실제 유니티 객체 여부
            { // 임시 관리자의 실제 제거
                Object.Destroy(manager.gameObject); // 원래 관리자와 게임 데이터 보존
            } // 임시 관리자의 실제 제거 종료
        } // 기존 회차 상태 복원 종료
    } // 별도 보유 원본 등록 준비 종료
} // 두 고철 공장 유물의 피해 검사 종료
