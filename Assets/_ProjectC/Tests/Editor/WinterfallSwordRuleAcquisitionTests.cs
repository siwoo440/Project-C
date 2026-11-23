using System.Collections; // 플레이 모드 전환 대기
using System.Collections.Generic; // 획득한 카드와 소유자 기록
using System.IO; // 탐사 파일과 연결된 그림 조회
using System.Linq; // 보물과 시트 원본 조회
using System.Reflection; // 보물 방의 고정 시드 계산 조회
using System.Security.Cryptography; // 원본 그림 해시 계산
using NUnit.Framework; // 검사 결과 대조
using UnityEditor; // 실제 등록 에셋 경로 확인
using UnityEngine; // 게임 보물과 소유자 객체 사용
using UnityEngine.TestTools; // 실제 탐사 저장 검사
using UnityEngine.UI; // 보물 소유자 선택 버튼

public sealed class WinterfallSwordRuleAcquisitionTests // 윈터폴 검 카드 여덟 종 원본과 획득 검사
{ // 검사 시작
    [TestCase(6, "CRITICAL_STRIKE", "치명 일격", "CriticalStrike", "21FF9BAD30297CC83563445A0B0E568D94FB8441AC91B254DAEDD99AE808AB6D")] // 치명 일격 원본
    [TestCase(7, "SWORD_DANCE", "검의 춤", "SwordDance", "982D7C84F598CD245EEF47AF918B95DD7F0B7249EAAAF23CB23EE894848F82E0")] // 검의 춤 원본
    [TestCase(8, "CHARGE", "돌진", "Charge", "7841898BCEBEC5B77A65033D6546542E64F957B375BF2576C3767B96904BCA07")] // 돌진 원본
    [TestCase(9, "COUNTERATTACK", "역공", "Counterattack", "3802D2590602096B64E967B0D6A01F53475D18C85223940B2E0B583690F9F21F")] // 역공 원본
    [TestCase(12, "FINAL_STRIKE", "최후의 일격", "FinalStrike", "74A6A36ED995A9ACE934AEFBCDA406D88F388A10A8E5D5B673B0FBD6448DC6B3")] // 최후의 일격 원본
    [TestCase(13, "INSPIRE_MORALE", "전의 고취", "InspireMorale", "327B9D3644EDC8CF0806884C135501111C097FAA1B1D1481A01C4D841174CAB1")] // 전의 고취 원본
    [TestCase(14, "FLAME_SLASH", "화염 참격", "FlameSlash", "05C566BD6B73D37AC3285630E58FD5590D0EE68CBD01A0CD63F49A2AE0D603C2")] // 화염 참격 원본
    [TestCase(16, "DOOM_BLADE", "파멸의 검", "DoomBlade", "A2133E7C277F0534D67A02F7FFE883DFE92AE8E1F8D7E3B6A3734FD47071FDF5")] // 파멸의 검 원본
    public void TreasureMatchesSwordSheetOriginalAndSavedArtwork(int row, string id, string name, string file, string digest) // 기획 행과 동명 그림 및 저장 가능한 보물 확인
    { // 원본 비교 시작
        var sheet = CardSheetCatalog.Load().FindByName(name); // 게임이 읽는 기획 행 조회
        var offer = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(item => item.OfferId == "REWARD_SHEET_" + id); // 실제 획득 후보 조회
        var card = offer.Card; // 보물 지급 카드 원본 조회
        Assert.IsTrue(offer.RewardOnly && offer.RequiresOwnerChoice); // 보물 전용과 선택 소유자 확인
        Assert.AreEqual(row, sheet.SourceRow); // 시트 행 번호 확인
        Assert.AreEqual(CardType.Sword, card.CardType); // 실제 검 속성 확인
        Assert.AreEqual(sheet.Name, card.DisplayName); // 원본 카드 이름 확인
        Assert.AreEqual(sheet.Description, card.Description); // 수정된 원문 효과 확인
        Assert.AreEqual("CRD_SHEET_" + id, card.CardId); // 저장 식별자 확인
        var fields = new SerializedObject(card); // 실제 직렬화 규칙 조회
        bool variableCost = fields.FindProperty("swordRule").intValue == 5; // 남은 행동력 전체를 쓰는 전투 규칙 확인
        Assert.AreEqual(sheet.Cost, variableCost ? "X" : card.ApCost.ToString()); // 가변 비용과 일반 비용을 기획 원본과 대조
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, card); // 탐사 저장과 복원에 사용하는 카드 확인
        Assert.AreEqual(name + ".png", sheet.ImageFileName); // ZIP의 원본 그림 파일명 확인
        string art = AssetDatabase.GetAssetPath(card.Artwork); // 실제 카드 스프라이트 파일 경로
        Assert.AreEqual("Assets/_ProjectC/Art/CardIllustrations/Sheet" + file + ".png", art); // 카드와 동명 그림 연결 확인
        using (var hash = SHA256.Create()) // 원본 그림 해시 비교 준비
        using (var picture = File.OpenRead(art)) // 실제 가져온 PNG 열기
        { // 그림 비교 시작
            Assert.AreEqual(digest, System.BitConverter.ToString(hash.ComputeHash(picture)).Replace("-", string.Empty)); // ZIP 원본과 PNG 내용 일치 확인
        } // 그림 비교 종료
    } // 원본 비교 종료

    [UnityTest] // 실제 보물 선택 및 파일 복원 검사 지정
    public IEnumerator EightTreasuresKeepTheirChosenAlternatingOwners() // 검 카드 여덟 장의 개별 소유자와 저장 상태 확인
    { // 실물 획득 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyChosenOwners(); // 실제 보물에서 두 파티원에게 교대로 지급
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실물 획득 검사 종료

    private static void VerifyChosenOwners() // 실제 탐사 경로로 검 여덟 장 획득
    { // 탐사 검사 시작
        var shop = PrepareExpedition(); // 생존 파티와 저장 가능한 회차 덱 구성
        var party = PartyLoadoutRuntime.Active.Party.Members; // 실제 출전한 소유자 후보 조회
        var pendingIds = new HashSet<string>(new[] { "CRD_SHEET_CRITICAL_STRIKE", "CRD_SHEET_SWORD_DANCE", "CRD_SHEET_CHARGE", "CRD_SHEET_COUNTERATTACK", "CRD_SHEET_FINAL_STRIKE", "CRD_SHEET_INSPIRE_MORALE", "CRD_SHEET_FLAME_SLASH", "CRD_SHEET_DOOM_BLADE" }); // 아직 획득하지 않은 카드
        var owners = new Dictionary<string, string>(); // 지급한 카드별 선택 소유자 기록
        var usedRooms = new List<string>(); // 저장에 포함할 실제 보물 방 기록
        string path = Path.Combine(Application.temporaryCachePath, "sword-owners-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 파일 경로
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 실제 보물의 고정 시드 함수 조회
        try // 검사 파일 정리 보장
        { // 보물 순회 시작
            for (int index = 0; index < 12000 && pendingIds.Count > 0; index++) // 서로 다른 방을 정해진 상한까지 순회
            { // 방 확인 시작
                string room = "winterfall-sword-224-" + index; // 이번 보물 방의 고유 식별자
                int seed = (int)hash.Invoke(null, new object[] { room }); // 해당 방의 보물 시드 조회
                if (ExplorationTreasureRewardService.RollRewardType(new System.Random(seed).Next(100)) != ExplorationTreasureRewardType.Card) // 실제 보물의 첫 추첨 종류 확인
                { // 카드 외 보물 시작
                    continue; // 다른 보상 지급 없이 다음 방 확인
                } // 카드 외 보물 종료
                Assert.IsTrue(ExplorationTreasureRewardService.TryGrantReward(room, out _, out var offer)); // 같은 방에서 선택을 기다리는 실제 카드 조회
                if (offer == null || !pendingIds.Remove(offer.Card.CardId)) // 이번에 확인할 카드가 맞는지 검사
                { // 다른 카드 제외 시작
                    continue; // 이미 획득한 카드와 다른 카드 제외
                } // 다른 카드 제외 종료
                var owner = party[owners.Count % 2]; // 두 출전 멤버 중 다음 소유자 선택
                int before = shop.RunDeck.CardCount; // 실제 소유자 확정 전 덱 크기 보관
                OpenTreasure(room, owner); // 실제 보물 창을 열고 소유자 버튼 선택
                Assert.AreEqual(before + 1, shop.RunDeck.CardCount); // 보물당 한 장 추가 확인
                var added = shop.RunDeck.Cards.Last(); // 선택으로 지급된 실제 덱 항목 조회
                Assert.AreSame(offer.Card, added.Card); // 미리 확인한 카드와 지급 카드 일치 확인
                Assert.AreSame(owner, added.Owner); // 선택한 파티원에게만 지급 확인
                owners.Add(added.Card.CardId, owner.CharacterId); // 저장 복원에 대조할 소유자 기록
                usedRooms.Add(room); // 지급을 마친 실제 방 기록
            } // 방 확인 종료
            CollectionAssert.IsEmpty(pendingIds); // 여덟 검을 모두 실제 보물에서 획득했는지 확인
            Assert.AreEqual(2, owners.Values.Distinct().Count()); // 두 소유자 모두 실제 지급받았는지 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 실제 탐사 파일 저장
            shop.RunDeck.ResetToSource(null); // 메모리 카드와 소유자 기록 제거
            ExplorationSessionManager.Instance.ResetExploration(); // 메모리 보물 방 기록 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 파일에서 탐사 상태 복원
            var restoredDeck = RunDeckManager.Instance; // 저장 파일에서 복원된 회차 덱 직접 조회
            Assert.IsNotNull(restoredDeck); // 상점을 방문하지 않은 탐사의 회차 덱 확인
            foreach (var pair in owners) // 지급한 검별 소유자 대조
            { // 복원 카드 확인 시작
                var restored = restoredDeck.Cards.Single(entry => entry.Card.CardId == pair.Key); // 복원된 회차 덱의 실제 카드 한 장 조회
                Assert.AreEqual(pair.Value, restored.Owner.CharacterId); // 원래 선택한 개별 소유자 확인
            } // 복원 카드 확인 종료
            Assert.IsTrue(usedRooms.All(ExplorationSessionManager.Instance.IsEventResolved)); // 복원된 보물 방의 재수령 차단 확인
        } // 보물 순회 종료
        finally // 독립 파일 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 검사 저장 파일 제거
        } // 정리 종료
    } // 탐사 검사 종료

    private static ShopRunManager PrepareExpedition() // 실제 파티와 독립 탐사 준비
    { // 탐사 준비 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 명단 조회
        PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 조건 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 번째 파티원 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 두 번째 파티원 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 편성과 시작 덱 생성
        ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 조회
        session.ResetExploration(); // 이전 탐사 상태 초기화
        session.SetCurrentFloorSeed(224); // 독립 탐사 층 시드 설정
        PlayerResourceManager.EnsureInstance().RestoreResources(1000000, 0, 0, 0); // 전체 상품 구매 골드 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 편성의 시작 덱 적용
        ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 조회
        Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 프로젝트 상품 원본 연결
        return shop; // 준비된 상점 반환
    } // 탐사 준비 종료

    private static void OpenTreasure(string id, CharacterData owner) // 탐사 보물 방의 실제 보상 획득
    { // 보물 지급 시작
        GameObject room = new GameObject(id); // 독립 보물 방 생성
        room.AddComponent<ExplorationSpecialRoomView>().Initialize(null, Vector2Int.zero, ExplorationRoomType.Treasure, id); // 실제 보물 동작 연결
        room.SendMessage("OpenTreasure"); // 보물 열기
        Transform chooser = room.transform.Find("TreasureOwnerCanvas"); // 공용 카드 선택 화면 조회
        if (chooser != null) // 카드 소유자 선택 필요 여부 확인
        { // 카드 소유자 선택 시작
            Assert.IsNotNull(owner, id); // 보물 카드 소유자 지정 확인
            Transform button = chooser.Find("Panel/Owner_" + owner.CharacterId); // 실제 소유자 버튼 조회
            Assert.IsNotNull(button, id); // 카드 선택 창 표시 확인
            button.GetComponent<Button>().onClick.Invoke(); // 파티원에게 카드 지급
        } // 카드 소유자 선택 종료
        Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(id), id); // 보물 지급과 사용 처리 확인
        Object.Destroy(room); // 독립 보물 방 정리
    } // 보물 지급 종료

} // 검사 종료
