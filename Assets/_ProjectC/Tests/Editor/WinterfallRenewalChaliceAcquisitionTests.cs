using System.Collections; // 플레이 모드 전환 대기
using System.Collections.Generic; // 획득 카드와 소유자 기록
using System.IO; // 검사 저장 파일 경로
using System.Linq; // 지급 카드와 복원 카드 조회
using System.Reflection; // 보물 방 시드 함수 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEngine; // 실제 탐사와 카드 객체 사용
using UnityEngine.TestTools; // 플레이 모드 획득 검사 등록
using UnityEngine.UI; // 실제 보물 소유자 버튼 사용
using static WinterfallCommonCardExpansionTests; // 공용 카드 원화 검사 재사용

public sealed class WinterfallRenewalChaliceAcquisitionTests // 신규 성배 카드의 원화와 획득 검사
{ // 성배 카드 검사 시작
    [TestCase(116, "CALMING", "진정", "Calming", "6A30E6013E711735CF2648686543D0BAE46046B590A7142D227C891E0C430D02")] // 진정 원화
    [TestCase(117, "HOLY_WATER", "성수 뿌리기", "HolyWater", "7F62ECE42008DD676BD9B01EF1E40BBF002F4B698D5F8CF3AA96778B4D2C4542")] // 성수 뿌리기 원화
    [TestCase(118, "HEALING_PRAYER", "회복 기도", "HealingPrayer", "13099A96978AC96181BED8D379FC4B5B228EFF862973D18F1BB414B66124C3AE")] // 회복 기도 원화
    [TestCase(119, "PURIFYING_TOUCH", "정화의 손길", "PurifyingTouch", "0CAA673BB99A34E4FD81E9D90916F8DEC253AC6EADA9AE2EE5B1345CAE9FADC7")] // 정화의 손길 원화
    [TestCase(120, "QUIET_BLESSING", "잔잔한 축복", "QuietBlessing", "56DDC21F3BEE664BB5CC141D06F3D45B4258AD66E333968DF649EC575566C04C")] // 잔잔한 축복 원화
    [TestCase(121, "LIGHT_FRAGMENT", "빛의 조각", "LightFragment", "E50274361B13F266DDB49FBFB45B4E25D79855874A45BCC67A1B9857CDB987F4")] // 빛의 조각 원화
    [TestCase(122, "REGENERATION", "재생", "Regeneration", "9CEE6524A3A08E316EE19D6D4115E6E890A8C55250FEF4088BD0597D9A13A313")] // 재생 원화
    [TestCase(124, "WOUND_STITCHING", "상처 봉합", "WoundStitching", "F4682B0A704431B872947013252D5C97AA35FB24408663469CCDA7E45F73C5B2")] // 상처 봉합 원화
    public void MatchesOriginalAndRestorableTreasure(int row, string id, string title, string assetName, string digest) // 카드 원화와 지급 설정 대조
    { // 원화 대조 시작
        VerifyTreasureArtwork(row, id, title, assetName, CardType.Cup, digest); // 시트 비용과 연결된 PNG 해시 확인
    } // 원화 대조 종료

    [UnityTest] // 실제 보물 버튼과 저장 복원 검사 등록
    public IEnumerator TreasuresRestoreTheirChosenChaliceOwners() // 신규 성배 카드의 두 가지 소유자 대조
    { // 실제 보물 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 관리자 사용 가능 상태 진입
        VerifyChosenOwners(); // 여덟 카드를 실제 보물로 획득하고 복원
        yield return new ExitPlayMode(); // 탐사 검사 상태 종료
    } // 실제 보물 검사 종료

    private static void VerifyChosenOwners() // 보물과 저장의 소유자 일치 확인
    { // 탐사 흐름 확인 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 캐릭터 명단 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 출전 상태 검사기 생성
        var selection = new PartySelection(recovery); // 새 출전 파티 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 소유자 후보 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 소유자 후보 편성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출전 파티와 시작 덱 생성
        var party = PartyLoadoutRuntime.Active.Party.Members; // 실제 출전 소유자 후보 조회
        var session = ExplorationSessionManager.EnsureInstance(); // 진행 중 탐사 상태 조회
        session.ResetExploration(); // 독립된 신규 탐사 상태 초기화
        session.SetCurrentFloorSeed(249); // 저장 복원용 층 시드 설정
        var deck = RunDeckManager.EnsureInstance(); // 실제 획득용 회차 덱 조회
        deck.ResetToSource(PartyLoadoutRuntime.Active.Deck); // 출전 덱을 적용한 상태 준비
        var pending = new HashSet<string> // 획득해야 할 신규 카드 식별자 목록
        { // 신규 카드 목록 시작
            "CRD_SHEET_CALMING", // 진정 식별자
            "CRD_SHEET_HOLY_WATER", // 성수 뿌리기 식별자
            "CRD_SHEET_HEALING_PRAYER", // 회복 기도 식별자
            "CRD_SHEET_PURIFYING_TOUCH", // 정화의 손길 식별자
            "CRD_SHEET_QUIET_BLESSING", // 잔잔한 축복 식별자
            "CRD_SHEET_LIGHT_FRAGMENT", // 빛의 조각 식별자
            "CRD_SHEET_REGENERATION", // 재생 식별자
            "CRD_SHEET_WOUND_STITCHING" // 상처 봉합 식별자
        }; // 신규 카드 목록 종료
        var owners = new Dictionary<string, string>(); // 보상별 선택 소유자 정리
        var openedRooms = new List<string>(); // 처리한 실제 보물 방 기록
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 실제 보물 추첨 시드 함수 조회
        Assert.IsNotNull(hash); // 보물 시드 함수 확인
        var hashParameters = new object[1]; // 방 식별자 전달용 인수 배열
        string path = Path.Combine(Application.temporaryCachePath, "renewal-chalice-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로
        try // 검사 파일 정리 보장
        { // 보물 획득 순회 시작
            for (int index = 0; index < 12000 && pending.Count > 0; index++) // 한도 안에서 서로 다른 보물 방 조회
            { // 방별 추첨 시작
                string id = "winterfall-shield-249-" + index; // 보물 방의 고정 식별자
                hashParameters[0] = id; // 현재 보물 방 식별자 전달
                int seed = (int)hash.Invoke(null, hashParameters); // 실제 보물 추첨 시드 계산
                if (ExplorationTreasureRewardService.RollRewardType(new System.Random(seed).Next(100)) != ExplorationTreasureRewardType.Card) // 카드 이외 첫 보상 확인
                { // 다른 종류 보물 제외 시작
                    continue; // 일반 재화와 유물 획득 방지
                } // 다른 종류 보물 제외 종료
                Assert.IsTrue(ExplorationTreasureRewardService.TryGrantReward(id, out _, out var offer)); // 이번 방의 실제 지급 후보 조회
                if (offer == null || !pending.Remove(offer.Card.CardId)) // 이미 확인한 카드나 이전 카드 제외
                { // 다른 카드 제외 시작
                    continue; // 다음 보물 방으로 진행
                } // 다른 카드 제외 종료
                var owner = party[owners.Count % 2]; // 두 출전 아군 중 교대로 선택
                int count = deck.CardCount; // 지급 전 카드 수 보관
                ChooseTreasureOwner(id, owner); // 실제 보물에서 소유자 선택
                Assert.AreEqual(count + 1, deck.CardCount); // 회차 덱에 카드 한 장 추가 확인
                var card = deck.Cards.Last(); // 실제 들어간 회차 덱 카드 조회
                Assert.AreSame(offer.Card, card.Card); // 보물 후보와 같은 카드 지급 확인
                Assert.AreSame(owner, card.Owner); // 선택한 파티원 소유 확인
                owners.Add(card.Card.CardId, owner.CharacterId); // 복원 뒤 대조할 소유자 기록
                openedRooms.Add(id); // 실제 지급이 끝난 보물 방 기록
            } // 방별 추첨 종료
            CollectionAssert.IsEmpty(pending); // 신규 여덟 장의 보물 획득 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 실제 탐사 저장 파일 생성
            deck.ResetToSource(null); // 회차 덱 카드 메모리 제거
            session.ResetExploration(); // 사용 완료 보물 메모리 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 탐사 저장 파일 복원
            foreach (var pair in owners) // 획득 카드와 복원 소유자 대조
            { // 개별 소유자 확인 시작
                var restored = RunDeckManager.Instance.Cards.Single(entry => entry.Card.CardId == pair.Key); // 복원한 실제 덱 항목 조회
                Assert.AreEqual(pair.Value, restored.Owner.CharacterId); // 저장 전후 카드 소유자 일치 확인
            } // 개별 소유자 확인 종료
            Assert.IsTrue(openedRooms.All(ExplorationSessionManager.Instance.IsEventResolved)); // 복원된 완료 방의 재수령 방지 확인
        } // 보물 획득 순회 종료
        finally // 임시 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 검사용 저장과 백업 제거
        } // 저장 파일 정리 종료
    } // 탐사 흐름 확인 종료

    private static void ChooseTreasureOwner(string id, CharacterData owner) // 실제 보물 방의 소유자 버튼 입력
    { // 보물 버튼 확인 시작
        var room = new GameObject(id); // 독립 보물 방 객체 생성
        try // 시험 보물 방 정리 보장
        { // 소유자 지급 시작
            room.AddComponent<ExplorationSpecialRoomView>().Initialize(null, Vector2Int.zero, ExplorationRoomType.Treasure, id); // 실제 보물 열기 동작 연결
            room.SendMessage("OpenTreasure"); // 열린 보물의 소유자 선택 표시
            Transform button = room.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + owner.CharacterId); // 실제 선택 후보 버튼 조회
            Assert.IsNotNull(button, id); // 선택 가능한 아군 버튼 확인
            button.GetComponent<Button>().onClick.Invoke(); // 선택 소유자에게 보물 카드 지급
            Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(id), id); // 지급을 마친 방의 처리 기록 확인
        } // 소유자 지급 종료
        finally // 독립 검사용 방 정리
        { // 시험실 정리 시작
            Object.Destroy(room); // 보물 오브젝트 제거
        } // 시험실 정리 종료
    } // 보물 버튼 확인 종료
} // 성배 카드 검사 종료
