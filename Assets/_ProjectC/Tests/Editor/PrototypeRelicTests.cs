using System.Collections.Generic; // 유물 효과 대상 목록 사용
using NUnit.Framework; // 유물 효과와 데이터 검증 사용
using UnityEngine; // 실제 유물 에셋 사용
public sealed class PrototypeRelicTests // 시제품 유물 효과 검사
{ // 유물 검사 시작
    [Test] // 잿빛 인장의 사용자 확인 방어도 검사 지정
    public void AshSeal_DistributesSixBlockAcrossLivingAllies() // 방어도 총량과 생존 순서 배분 확인
    { // 잿빛 인장 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(source); // 첫 아군 생성
        BattleUnitRuntime dead = BattleUnitRuntime.CreateAlly(source); // 사망 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(source); // 두 번째 생존 아군 생성
        dead.ApplyPersistentHealth(0); // 가운데 아군 사망 상태 적용
        ApplyInitialRelic("AshSeal", new[] { first, dead, second }); // 실제 인장의 전투 시작 효과 적용
        Assert.AreEqual(3, first.CurrentBlock); // 첫 생존 아군 방어도 확인
        Assert.AreEqual(0, dead.CurrentBlock); // 사망 아군 배분 제외 확인
        Assert.AreEqual(3, second.CurrentBlock); // 두 번째 생존 아군 방어도 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 잿빛 인장 검사 종료
    [Test] // 전체 정신력과 저항 유물 검사 지정
    public void TeamRelics_ApplyMentalAndResistanceToLivingAllies() // 전체 아군 정신력과 저항 효과 확인
    { // 전체 아군 유물 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(source); // 첫 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(source); // 두 번째 아군 생성
        BattleUnitRuntime[] allies = { first, second }; // 두 생존 아군 목록 생성
        int initialMental = first.CurrentMental; // 기존 정신력 기록
        ApplyInitialRelic("BrokenMusicBox", allies); // 실제 오르골 전투 시작 효과 적용
        Assert.AreEqual(initialMental + 1, first.CurrentMental); // 첫 아군 정신력 증가 확인
        Assert.AreEqual(initialMental + 1, second.CurrentMental); // 두 번째 아군 정신력 증가 확인
        ApplyInitialRelic("BronzeCenser", allies); // 실제 향로 전투 시작 효과 적용
        Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.MagicalResistanceUp).EffectiveValue); // 첫 아군 저항 증가 확인
        Assert.AreEqual(1, second.GetStatusEffect(BattleStatusEffectType.MagicalResistanceUp).RemainingTurns); // 두 번째 아군 지속 한 턴 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 전체 아군 유물 검사 종료
    [Test] // 무작위 생존 아군 유물 검사 지정
    public void FloatingStone_GivesEightBlockToExactlyOneLivingAlly() // 사망자를 제외한 단일 대상 확인
    { // 부유석 유물 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(source); // 첫 아군 생성
        BattleUnitRuntime dead = BattleUnitRuntime.CreateAlly(source); // 사망 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(source); // 두 번째 생존 아군 생성
        dead.ApplyPersistentHealth(0); // 가운데 아군 사망 상태 적용
        ApplyInitialRelic("FloatingStone", new[] { first, dead, second }); // 실제 부유석 전투 시작 효과 적용
        Assert.AreEqual(8, first.CurrentBlock + second.CurrentBlock); // 생존 아군의 총 방어도 확인
        Assert.IsTrue(first.CurrentBlock == 8 || second.CurrentBlock == 8); // 생존 아군 한 명에게만 적용 확인
        Assert.AreEqual(0, dead.CurrentBlock); // 사망 아군 제외 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 부유석 유물 검사 종료
    [Test] // 인증키 방어도 검사 지정
    public void GateKey_GivesSixBlockToEveryLivingAlly() // 생존 아군 전체 방어도 확인
    { // 인증키 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(source); // 첫 생존 아군 생성
        BattleUnitRuntime dead = BattleUnitRuntime.CreateAlly(source); // 사망 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(source); // 두 번째 생존 아군 생성
        dead.ApplyPersistentHealth(0); // 사망 상태 적용
        ApplyInitialRelic("GateKey", new[] { first, dead, second }); // 인증키 전투 시작 효과 적용
        Assert.AreEqual(6, first.CurrentBlock); // 첫 생존 아군 방어도 확인
        Assert.AreEqual(0, dead.CurrentBlock); // 사망 아군 제외 확인
        Assert.AreEqual(6, second.CurrentBlock); // 두 번째 생존 아군 방어도 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 인증키 검사 종료
    [Test] // 실제 유물의 획득과 저장 등록 검사 지정
    public void Relics_AreUniqueAndRegisteredForShopAndSave() // 유물 원본과 상점 및 저장 목록 확인
    { // 유물 등록 검사 시작
        string[] names = { "AshSeal", "BrokenMusicBox", "BronzeCenser", "FloatingStone", "GateKey", "CrackedLens", "SealedEnvelope", "FadedRune" }; // 검사할 유물 경로 이름
        HashSet<string> ids = new HashSet<string>(); // 고유 유물 식별자 집합 생성
        SaveAssetCatalog save = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장 원본 목록 조회
        ShopCatalogData shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 상품 목록 조회
        Assert.IsNotNull(save); // 저장 원본 목록 확인
        Assert.IsNotNull(shop); // 상점 상품 목록 확인
        foreach (string name in names) // 실제 유물 경로 순회
        { // 개별 유물 등록 확인 시작
            RelicData relic = Resources.Load<RelicData>("Relics/" + name); // 실제 유물 원본 조회
            Assert.IsNotNull(relic, name); // 유물 원본 존재 확인
            Assert.IsTrue(relic.IsValidData(), name); // 유물 원본 유효성 확인
            Assert.IsTrue(ids.Add(relic.RelicId), name); // 유물 식별자 중복 방지 확인
            Assert.IsNotNull(relic.Icon, name); // 유물 그림 연결 확인
            CollectionAssert.Contains(save.Relics, relic); // 저장 복원 목록 연결 확인
            bool inShop = false; // 실제 상점 등록 여부 초기화
            foreach (ShopOfferData offer in shop.Offers) // 상점 상품 순회
            { // 유물 상품 확인 시작
                inShop |= offer.OfferType == ShopOfferType.Relic && offer.Relic == relic && offer.IsValidData(); // 유효한 실제 유물 상품 확인
            } // 유물 상품 확인 종료
            Assert.IsTrue(inShop, name); // 상점과 보물 후보 등록 확인
        } // 개별 유물 등록 확인 종료
    } // 유물 등록 검사 종료
    private static void ApplyInitialRelic(string path, IReadOnlyList<BattleUnitRuntime> allies) // 실제 유물의 전투 시작 효과 적용
    { // 유물 효과 실행 시작
        RelicData relic = Resources.Load<RelicData>("Relics/" + path); // 실제 유물 원본 조회
        Assert.IsNotNull(relic); // 유물 원본 존재 확인
        RelicGoldRuntime gold = new RelicGoldRuntime(); // 독립 유물 지갑 생성
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(gold); // 독립 유물 보관함 생성
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 유물 실제 획득 확인
        BattleEventDispatcher events = new BattleEventDispatcher(); // 독립 전투 이벤트 생성
        BattleRelicEffectController controller = new BattleRelicEffectController(events, inventory, gold, allies, new BattleUnitRuntime[0]); // 실제 유물 효과 관리자 생성
        controller.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 실제 전투 시작 효과 처리
        controller.Dispose(); // 유물 효과 관리자 정리
        events.Dispose(); // 전투 이벤트 정리
    } // 유물 효과 실행 종료
} // 유물 검사 종료
