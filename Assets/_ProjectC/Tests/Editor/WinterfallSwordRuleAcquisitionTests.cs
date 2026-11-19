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

public sealed class WinterfallSwordRuleAcquisitionTests // 윈터폴 검 카드 마흔다섯 종 원본과 획득 검사
{ // 검사 시작
    [TestCase(6, "CRITICAL_STRIKE", "치명 일격", "CriticalStrike", "21FF9BAD30297CC83563445A0B0E568D94FB8441AC91B254DAEDD99AE808AB6D")] // 치명 일격 원본
    [TestCase(7, "SWORD_DANCE", "검의 춤", "SwordDance", "982D7C84F598CD245EEF47AF918B95DD7F0B7249EAAAF23CB23EE894848F82E0")] // 검의 춤 원본
    [TestCase(8, "CHARGE", "돌진", "Charge", "7841898BCEBEC5B77A65033D6546542E64F957B375BF2576C3767B96904BCA07")] // 돌진 원본
    [TestCase(9, "COUNTERATTACK", "역공", "Counterattack", "3802D2590602096B64E967B0D6A01F53475D18C85223940B2E0B583690F9F21F")] // 역공 원본
    [TestCase(12, "FINAL_STRIKE", "최후의 일격", "FinalStrike", "74A6A36ED995A9ACE934AEFBCDA406D88F388A10A8E5D5B673B0FBD6448DC6B3")] // 최후의 일격 원본
    [TestCase(13, "INSPIRE_MORALE", "전의 고취", "InspireMorale", "327B9D3644EDC8CF0806884C135501111C097FAA1B1D1481A01C4D841174CAB1")] // 전의 고취 원본
    [TestCase(14, "FLAME_SLASH", "화염 참격", "FlameSlash", "05C566BD6B73D37AC3285630E58FD5590D0EE68CBD01A0CD63F49A2AE0D603C2")] // 화염 참격 원본
    [TestCase(16, "DOOM_BLADE", "파멸의 검", "DoomBlade", "A2133E7C277F0534D67A02F7FFE883DFE92AE8E1F8D7E3B6A3734FD47071FDF5")] // 파멸의 검 원본
    [TestCase(17, "THRUST", "찌르기", "Thrust", "BD65E93EC4C9984CE5B4560E3125D2A5683B0BDE9E94167B5710120F0C6E08B6")] // 찌르기 원본
    [TestCase(18, "HORIZONTAL_SLASH", "가로베기", "HorizontalSlash", "1F488127E7400B6458658A5F995262E3A2A6BB401705A0E7BFF3C001973C9854")] // 가로베기 원본
    [TestCase(19, "KNEE_SLASH", "무릎 베기", "KneeSlash", "5F996D7B963DAC5AD7AAB36EDCC84DEFAC70CC7177BD6E37E3429549FCD82964")] // 무릎 베기 원본
    [TestCase(20, "POMMEL_STRIKE", "검등 치기", "PommelStrike", "7E1C7705180680EE36EB5D6107C4F81DE8E6E2F5547B933775B255BB02F5BFE2")] // 검등 치기 원본
    [TestCase(22, "BLOOD_EDGE", "피의 칼끝", "BloodEdge", "E4A2149F7D816A3AA89CF1938CD5123A6D73CDA081FCD9D641DE133E42A9CC45")] // 피의 칼끝 원본
    [TestCase(23, "SHIELD_SPLITTER", "방패 쪼개기", "ShieldSplitter", "016322B10602A0B8E72CE7D3E883AB3987224E7AFB2813D8FE5ECF27226550D6")] // 방패 쪼개기 원본
    [TestCase(24, "INTERCEPT_SLASH", "저지 베기", "InterceptSlash", "AD52ADAA22F20F325C1DEECEF8CE0A42F454EFCD5C82C31101D83724F73D3F75")] // 저지 베기 원본
    [TestCase(25, "CHECK_SLASH", "견제 참격", "CheckSlash", "E42D1B813455528D7C3945210D2C8F23AE37E895BC16ED364E7E371B016F495D")] // 견제 참격 원본
    [TestCase(26, "VITAL_AIM", "급소 노림", "VitalAim", "CAC20B48D6D58E409074FBF3A5D2EDA09F550D8A5CB01579C8AEA8FA6173082C")] // 급소 노림 원본
    [TestCase(27, "BLADE_MAINTENANCE", "칼날 정비", "BladeMaintenance", "B101E796B32F9F483A2146B0DBF4E6200AB34FD54FD976FE5EAD23AF66AB53C8")] // 칼날 정비 원본
    [TestCase(28, "FRONTLINE_ORDER", "전열 정돈", "FrontlineOrder", "715E7E2402E1F680D36900F7D32CAFA38CD6144D66888ECBB6FE0419D0BF55D8")] // 전열 정돈 원본
    [TestCase(30, "EXECUTION_SLASH", "처형 베기", "ExecutionSlash", "0C5C7AB4FA313980F297C6FA35BE01DD387FF0349A4F5DCDAAB05204083B5910")] // 처형 베기 원본
    [TestCase(31, "BLOOD_GALE", "피바람", "BloodGale", "35B7AC55A2FF71EBD0917B35511620A4F91CC4A8FD8496271922F1995F912BD5")] // 피바람 원본
    [TestCase(32, "PHYSICAL_RIPOSTE", "검격 반사", "PhysicalRiposte", "A2C176F41E4ACBF6BA7F0B560F911E1CBE3F44AA91F63A0DAE9F5CBB85223291")] // 검격 반사 원본
    [TestCase(33, "PIERCING_LUNGE", "파고들기", "PiercingLunge", "873ABEE8622DA646C500AF95E270BD40495EB012AD8F6BDE6D135FBC5C7A1EA9")] // 파고들기 원본
    [TestCase(34, "TWIN_BLADE_FLURRY", "쌍검 연무", "TwinBladeFlurry", "ABEE9112CB30E075FD0708F1BCA536DA47DE415BA76D72AF6918C33655F3EC7B")] // 쌍검 연무 원본
    [TestCase(35, "BLOOD_OATH", "피의 맹세", "BloodOath", "44A82B4DC9BF8E3379C8D238157CFB01974BD9D441C4B5366F1C37E217102BE3")] // 피의 맹세 원본
    [TestCase(36, "DEFENSE_BREAK", "방어 붕괴", "DefenseBreak", "EAE1D709F0C2A11C25360A01579ACCA12D0C16D1C345549FDEFC7B58DD027055")] // 방어 붕괴 원본
    [TestCase(37, "INTIMIDATING_CHARGE", "위협 돌진", "IntimidatingCharge", "0B39BAC483E7463C7BDA3C27682C8A792CCFC049E9914E0EC34FEC9E5B4E55A3")] // 위협 돌진 원본
    [TestCase(38, "BLADE_WHIRLWIND", "칼날 소용돌이", "BladeWhirlwind", "B3F780C31C714769029F7826300893F372B295A3E3D83D3B7216D60E60D589B2")] // 칼날 소용돌이 원본
    [TestCase(39, "FINISHING_STANCE", "마무리 자세", "FinishingStance", "1B682A44757F5B26968940494BC33BE5A4681E18532348AB8744CEFBF29066D5")] // 마무리 자세 원본
    [TestCase(40, "BLOOD_TRAIL", "피의 추적", "BloodTrail", "CBDF2814F4168F27E2F313C7DDEE13ACB79168CC0A578D13EA7D27E89503E3A5")] // 피의 추적 원본
    [TestCase(41, "SEVERING_STRIKE", "절단 일격", "SeveringStrike", "DDA9E5B949811382D9FBD32852AC848EFEB56F9D0BB2CA1DA2609A39AA1B26C8")] // 절단 일격 원본
    [TestCase(42, "BREAKTHROUGH_SLASH", "돌파 참격", "BreakthroughSlash", "89BBB83D610A8E9035D4CE63B6D50F3972176F5B79418A86A170691A2672AD03")] // 돌파 참격 원본
    [TestCase(43, "RAGE_RELEASE", "분노 해방", "RageRelease", "F12B50D09694DC7C974EA33CF19FE3E0C5BF1A7B0985C59F44949B24A3C316A9")] // 분노 해방 원본
    [TestCase(44, "EXECUTIONER_SWORD", "처단의 검", "ExecutionerSword", "90B036A6337A2DAE4480A10B938FE3CABD7FA55FFFA096B89158B76AF85F07A4")] // 처단의 검 원본
    [TestCase(45, "RIFT_SLASH", "균열 베기", "RiftSlash", "8FE5209EF7FC1456016E26709B3DDC23336A1878D90D89533490913E4B9F2B4B")] // 균열 베기 원본
    [TestCase(47, "BATTLEFIELD_RESOLVE", "전장의 결의", "BattlefieldResolve", "549997FDAD90D1F18D1888E7CD76ED157E9369F8DC7184FDE2207AEC66BE1555")] // 전장의 결의 원본
    [TestCase(48, "BLOOD_BACKFLOW", "피의 역류", "BloodBackflow", "DF9D0FF9A2621AE2C586DEC3B25F5715463C65CE829A6A03D624257CCE3CC6F8")] // 피의 역류 원본
    [TestCase(49, "BLADE_FRENZY", "칼날 폭주", "BladeFrenzy", "AE7355AF8D0692CF2D3299B59BC27458FD2AD8A286C4319EFEEDF508551E1606")] // 칼날 폭주 원본
    [TestCase(50, "JUDGMENT_SLASH", "단죄 베기", "JudgmentSlash", "622D8FA4DF155F5C62FEA282F7C68CBCBD54DDDFD6ED98460C09D2E3528BFB83")] // 단죄 베기 원본
    [TestCase(51, "KINGS_SLASH", "왕의 참격", "KingsSlash", "1CA6344D7E2EBB288833DB83A8CA6A9E51AB006883D84C03075BB5D099966647")] // 왕의 참격 원본
    [TestCase(52, "BLOODSTAINED_DUEL", "피로 물든 결투", "BloodstainedDuel", "A062724FE58B8048D293EB5267D5BCAC8E904ED82123F4F2602CF6AA549786D9")] // 피로 물든 결투 원본
    [TestCase(53, "SWORD_SAINT_STANCE", "검성의 자세", "SwordSaintStance", "095EABAD7E4E3B8E8E2DE7AD7708EE2EDD0DE817778D7B48EEF3BAC72AC8E59B")] // 검성의 자세 원본
    [TestCase(54, "LAST_BREAKTHROUGH", "최후 돌파", "LastBreakthrough", "10AA0C9A476C8BE3BF501CABBD26E482AD098448D72923A7B630FE44B1579F07")] // 최후 돌파 원본
    [TestCase(55, "HEAVENLY_SWORD_FLURRY", "천검 난무", "HeavenlySwordFlurry", "AE5E0D9D0D8EC687BFFBE05FACC82A691A63AB04388303F07BC10BC0AFE213AA")] // 천검 난무 원본
    [TestCase(56, "DOOM_SEVERANCE", "파멸 절단", "DoomSeverance", "5AC59FF5D56EB6F0156C6328876D75910B6FA98BE48DBF69EF398663F2A93C43")] // 파멸 절단 원본
    public void TreasureMatchesSwordSheetOriginalAndSavedArtwork(int row, string id, string name, string file, string digest) // 기획 행과 동명 그림 및 저장 가능한 보물 확인
    { // 원본 비교 시작
        var sheet = CardSheetCatalog.Load().FindByName(name); // 게임이 읽는 기획 행 조회
        var offer = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.SingleOrDefault(item => item.OfferId == "REWARD_SHEET_" + id); // 실제 획득 후보 조회
        Assert.IsNotNull(offer, "보물 획득 후보 누락: " + id); // 보상 등록 여부 확인
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
    public IEnumerator TreasuresKeepTheirChosenAlternatingOwners() // 검 카드 마흔다섯 장의 개별 소유자와 저장 상태 확인
    { // 실물 획득 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyChosenOwners(); // 실제 보물에서 두 파티원에게 교대로 지급
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실물 획득 검사 종료

    private static void VerifyChosenOwners() // 실제 탐사 경로로 검 마흔다섯 장 획득
    { // 탐사 검사 시작
        var shop = PrepareExpedition(); // 생존 파티와 저장 가능한 회차 덱 구성
        var party = PartyLoadoutRuntime.Active.Party.Members; // 실제 출전한 소유자 후보 조회
        var pendingIds = new HashSet<string> // 획득과 저장 복원을 검사할 검 카드 식별자
        { // 검 카드 목록 시작
            "CRD_SHEET_CRITICAL_STRIKE", // 치명 일격 원본 번호
            "CRD_SHEET_SWORD_DANCE", // 검의 춤 원본 번호
            "CRD_SHEET_CHARGE", // 돌진 원본 번호
            "CRD_SHEET_COUNTERATTACK", // 역공 원본 번호
            "CRD_SHEET_FINAL_STRIKE", // 최후의 일격 원본 번호
            "CRD_SHEET_INSPIRE_MORALE", // 전의 고취 원본 번호
            "CRD_SHEET_FLAME_SLASH", // 화염 참격 원본 번호
            "CRD_SHEET_DOOM_BLADE", // 파멸의 검 원본 번호
            "CRD_SHEET_THRUST", // 찌르기 원본 번호
            "CRD_SHEET_HORIZONTAL_SLASH", // 가로베기 원본 번호
            "CRD_SHEET_KNEE_SLASH", // 무릎 베기 원본 번호
            "CRD_SHEET_POMMEL_STRIKE", // 검등 치기 원본 번호
            "CRD_SHEET_BLOOD_EDGE", // 피의 칼끝 원본 번호
            "CRD_SHEET_SHIELD_SPLITTER", // 방패 쪼개기 원본 번호
            "CRD_SHEET_INTERCEPT_SLASH", // 저지 베기 원본 번호
            "CRD_SHEET_CHECK_SLASH", // 견제 참격 원본 번호
            "CRD_SHEET_VITAL_AIM", // 급소 노림 원본 번호
            "CRD_SHEET_BLADE_MAINTENANCE", // 칼날 정비 원본 번호
            "CRD_SHEET_FRONTLINE_ORDER", // 전열 정돈 원본 번호
            "CRD_SHEET_EXECUTION_SLASH", // 처형 베기 원본 번호
            "CRD_SHEET_BLOOD_GALE", // 피바람 원본 번호
            "CRD_SHEET_PHYSICAL_RIPOSTE", // 검격 반사 원본 번호
            "CRD_SHEET_PIERCING_LUNGE", // 파고들기 원본 번호
            "CRD_SHEET_TWIN_BLADE_FLURRY", // 쌍검 연무 원본 번호
            "CRD_SHEET_BLOOD_OATH", // 피의 맹세 원본 번호
            "CRD_SHEET_DEFENSE_BREAK", // 방어 붕괴 원본 번호
            "CRD_SHEET_INTIMIDATING_CHARGE", // 위협 돌진 원본 번호
            "CRD_SHEET_BLADE_WHIRLWIND", // 칼날 소용돌이 원본 번호
            "CRD_SHEET_FINISHING_STANCE", // 마무리 자세 원본 번호
            "CRD_SHEET_BLOOD_TRAIL", // 피의 추적 원본 번호
            "CRD_SHEET_SEVERING_STRIKE", // 절단 일격 원본 번호
            "CRD_SHEET_BREAKTHROUGH_SLASH", // 돌파 참격 원본 번호
            "CRD_SHEET_RAGE_RELEASE", // 분노 해방 원본 번호
            "CRD_SHEET_EXECUTIONER_SWORD", // 처단의 검 원본 번호
            "CRD_SHEET_RIFT_SLASH", // 균열 베기 원본 번호
            "CRD_SHEET_BATTLEFIELD_RESOLVE", // 전장의 결의 원본 번호
            "CRD_SHEET_BLOOD_BACKFLOW", // 피의 역류 원본 번호
            "CRD_SHEET_BLADE_FRENZY", // 칼날 폭주 원본 번호
            "CRD_SHEET_JUDGMENT_SLASH", // 단죄 베기 원본 번호
            "CRD_SHEET_KINGS_SLASH", // 왕의 참격 원본 번호
            "CRD_SHEET_BLOODSTAINED_DUEL", // 피로 물든 결투 선택 소유자
            "CRD_SHEET_SWORD_SAINT_STANCE", // 검성의 자세 선택 소유자
            "CRD_SHEET_LAST_BREAKTHROUGH", // 최후 돌파 선택 소유자
            "CRD_SHEET_HEAVENLY_SWORD_FLURRY", // 천검 난무 선택 소유자
            "CRD_SHEET_DOOM_SEVERANCE", // 파멸 절단 선택 소유자
        }; // 검 카드 목록 종료
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
            CollectionAssert.IsEmpty(pendingIds); // 마흔다섯 검을 모두 실제 보물에서 획득했는지 확인
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
