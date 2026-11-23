using System.Collections; // 플레이 모드 전환 대기
using System.Collections.Generic; // 획득 카드와 소유자 기록
using System.IO; // 검사 저장 파일 경로
using System.Linq; // 지급 카드와 복원 카드 조회
using System.Reflection; // 보물 방의 고정 시드 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEngine; // 실제 탐사 객체 사용
using UnityEngine.TestTools; // 플레이 모드 획득 검사
using UnityEngine.UI; // 보물 소유자 선택 버튼
using static WinterfallCommonCardExpansionTests; // 공용 카드 원본 검사 재사용

public sealed class WinterfallShieldAcquisitionTests // 방패 카드 원본과 실제 획득 검사
{ // 검사 시작
    [TestCase(61, "BODY_GUARD", "몸으로 막기", "BodyGuard", "BF05CE76EB59E6C5DAA7DC462D918435FDE64728A3EFC1833FD9F9FBDC457D30")] // 몸으로 막기 원본
    [TestCase(63, "SHOCK_ABSORPTION", "충격 흡수", "ShockAbsorption", "00A4EBB4378FF28A9E06141EB350C6272ED2A312AE288C5F4A210CDFCE7509A3")] // 충격 흡수 원본
    [TestCase(64, "DEFENSE_ORDER", "수비 명령", "DefenseOrder", "1A8D6DAD07592003D7A1D8B00E5898AAE72BBF70ACC1CA1753F2926D5DCAF7D0")] // 수비 명령 원본
    [TestCase(65, "STURDY_STANCE", "견고한 자세", "SturdyStance", "45362F994127B658D3DC9321C959E25D46B784F4A39E183A5001DD2D164A87FB")] // 견고한 자세 원본
    [TestCase(66, "SHIELD_BASH", "방패 밀치기", "ShieldBash", "BDF398EA5F6AE81602F6319F16A4D48C15A9988EA5F82C1D852AFF5EE461A080")] // 방패 밀치기 원본
    [TestCase(67, "GUARD_SWITCH", "가드 전환", "GuardSwitch", "AFE7D6BEBA18AFACC30D0EB50A5D694659618950E2F94E53F5A111791A046E9A")] // 가드 전환 원본
    [TestCase(68, "HOLD_BACK", "막아 세우기", "HoldBack", "ABC3C145DAB8ACCA602ED3358063EDA31343CA250FA7D616401A8463EC09DE09")] // 막아 세우기 원본
    [TestCase(69, "RESISTANCE_STANCE", "저항 태세", "ResistanceStance", "1E3E968EEB2C9E076325A5633F351705698B179A9EA9D16F31660B5DC1B122D7")] // 저항 태세 원본
    public void MatchesOriginalAndRestorableTreasure(int row, string id, string title, string assetName, string digest) // 시트와 보물 및 저장 원본 확인
    { // 원본 검사 시작
        VerifyTreasureArtwork(row, id, title, assetName, CardType.Shield, digest); // 실제 지급 설정과 원화 해시 대조
    } // 원본 검사 종료

    [UnityTest] // 실제 보물 선택과 저장 복원 검사 지정
    public IEnumerator TreasuresRestoreTheirChosenShieldOwners() // 여덟 방패 카드의 지급과 복원 확인
    { // 실물 획득 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyChosenOwners(); // 두 파티원에게 교대로 지급한 카드 검사
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실물 획득 검사 종료

    private static void VerifyChosenOwners() // 보물 획득과 탐사 저장 복원
    { // 탐사 검사 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 파티 명단 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 출전 가능 상태 준비
        var selection = new PartySelection(recovery); // 독립 파티 편성 생성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 소유자 후보 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 소유자 후보 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 편성과 시작 덱 준비
        var party = PartyLoadoutRuntime.Active.Party.Members; // 실제 출전 소유자 후보 조회
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 탐사 상태 조회
        session.ResetExploration(); // 독립 탐사 시작
        session.SetCurrentFloorSeed(237); // 탐사 저장용 층 시드 지정
        var deck = RunDeckManager.EnsureInstance(); // 실제 회차 덱 조회
        deck.ResetToSource(PartyLoadoutRuntime.Active.Deck); // 시작 덱 적용
        var pending = new HashSet<string> // 실제로 획득할 고정 카드 식별자
        { // 방패 카드 목록 시작
            "CRD_SHEET_BODY_GUARD", // 몸으로 막기 식별자
            "CRD_SHEET_SHOCK_ABSORPTION", // 충격 흡수 식별자
            "CRD_SHEET_DEFENSE_ORDER", // 수비 명령 식별자
            "CRD_SHEET_STURDY_STANCE", // 견고한 자세 식별자
            "CRD_SHEET_SHIELD_BASH", // 방패 밀치기 식별자
            "CRD_SHEET_GUARD_SWITCH", // 가드 전환 식별자
            "CRD_SHEET_HOLD_BACK", // 막아 세우기 식별자
            "CRD_SHEET_RESISTANCE_STANCE" // 저항 태세 식별자
        }; // 방패 카드 목록 종료
        var owners = new Dictionary<string, string>(); // 카드별로 선택한 소유자 기록
        var openedRooms = new List<string>(); // 실제로 사용한 보물 방 기록
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 보물 방 시드 계산 함수 조회
        Assert.IsNotNull(hash); // 보물 방 시드 계산 함수 확인
        string path = Path.Combine(Application.temporaryCachePath, "shield-owners-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 파일 경로
        try // 검사 파일 정리 보장
        { // 보물 순회 시작
            for (int index = 0; index < 12000 && pending.Count > 0; index++) // 한도 안에서 서로 다른 보물 방 탐색
            { // 보물 방 확인 시작
                string id = "winterfall-shield-237-" + index; // 이번 보물 방의 고정 식별자
                int seed = (int)hash.Invoke(null, new object[] { id }); // 실제 보물 난수 시드 조회
                if (ExplorationTreasureRewardService.RollRewardType(new System.Random(seed).Next(100)) != ExplorationTreasureRewardType.Card) // 카드 이외의 첫 보상 확인
                { // 카드 외 보물 시작
                    continue; // 다른 보상 지급 방지
                } // 카드 외 보물 종료
                Assert.IsTrue(ExplorationTreasureRewardService.TryGrantReward(id, out _, out var offer)); // 현재 보물의 소유자 선택 대상 조회
                if (offer == null || !pending.Remove(offer.Card.CardId)) // 이미 검사했거나 다른 보물 카드 확인
                { // 다른 카드 제외 시작
                    continue; // 다음 보물 방 확인
                } // 다른 카드 제외 종료
                var owner = party[owners.Count % 2]; // 두 파티원 중 다음 소유자 선택
                int count = deck.CardCount; // 소유자 선택 전 덱 크기 기록
                ChooseTreasureOwner(id, owner); // 실제 보물의 소유자 버튼 클릭
                Assert.AreEqual(count + 1, deck.CardCount); // 보물에서 공용 카드 한 장 지급 확인
                var awarded = deck.Cards.Last(); // 실제로 추가된 카드 조회
                Assert.AreSame(offer.Card, awarded.Card); // 보물 후보와 지급 카드 일치 확인
                Assert.AreSame(owner, awarded.Owner); // 지급 카드의 선택 소유자 확인
                owners.Add(awarded.Card.CardId, owner.CharacterId); // 저장과 대조할 소유자 기록
                openedRooms.Add(id); // 저장과 대조할 완료 방 기록
            } // 보물 방 확인 종료
            CollectionAssert.IsEmpty(pending); // 여덟 카드의 실제 보물 지급 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 실제 탐사 파일 저장
            deck.ResetToSource(null); // 메모리의 회차 카드 제거
            session.ResetExploration(); // 메모리의 보물 방 기록 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 탐사 파일 복원
            foreach (var pair in owners) // 카드별 저장 소유자 대조
            { // 카드 복원 확인 시작
                var restored = RunDeckManager.Instance.Cards.Single(entry => entry.Card.CardId == pair.Key); // 복원된 카드 한 장 조회
                Assert.AreEqual(pair.Value, restored.Owner.CharacterId); // 복원 전후 소유자 일치 확인
            } // 카드 복원 확인 종료
            Assert.IsTrue(openedRooms.All(ExplorationSessionManager.Instance.IsEventResolved)); // 복원된 보물의 중복 수령 방지 확인
        } // 보물 순회 종료
        finally // 독립 탐사 파일 정리
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 검사 저장 파일 제거
        } // 파일 정리 종료
    } // 탐사 검사 종료

    private static void ChooseTreasureOwner(string id, CharacterData owner) // 실제 보물 방의 소유자 버튼 입력
    { // 보물 지급 검사 시작
        var room = new GameObject(id); // 독립 보물 방 생성
        try // 검사 방 정리 보장
        { // 보물 동작 시작
            room.AddComponent<ExplorationSpecialRoomView>().Initialize(null, Vector2Int.zero, ExplorationRoomType.Treasure, id); // 실제 보물 방 동작 연결
            room.SendMessage("OpenTreasure"); // 탐사 보물 열기
            Transform button = room.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + owner.CharacterId); // 선택한 소유자의 실제 버튼 조회
            Assert.IsNotNull(button, id); // 소유자 선택 버튼 확인
            button.GetComponent<Button>().onClick.Invoke(); // 소유자 선택과 카드 지급
            Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(id), id); // 보물 방 처리 완료 확인
        } // 보물 동작 종료
        finally // 독립 보물 방 정리
        { // 방 정리 시작
            Object.Destroy(room); // 검사 보물 방 제거
        } // 방 정리 종료
    } // 보물 지급 검사 종료
} // 검사 종료
