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

public sealed class WinterfallDownpourAndSurgeAcquisitionTests // 신규 지팡이 카드의 원화와 획득 검사
{ // 지팡이 카드 검사 시작
    [TestCase(185, "FIRE_DOWNPOUR", "화염 폭우", "FireDownpour", "88A45A4FC0825C8F8D092528B50DD83CC447918CAC70700E29B0B9B759513571")] // 화염 폭우 원본과 그림 해시 대조
    [TestCase(186, "THUNDER_PRISON", "천둥 감옥", "ThunderPrison", "A0C2D304613238E1C326F22D8540D5D812839BAE89BB1186BD5304104CFE0854")] // 천둥 감옥 원본과 그림 해시 대조
    [TestCase(187, "MANA_OVERLOAD", "마력 폭주", "ManaOverload", "F601BA3908E205BC445B82BBC8D489022FB498066CCE6C15415158ED16B435F9")] // 마력 폭주 원본과 그림 해시 대조
    [TestCase(188, "VOID_HAND", "공허의 손", "VoidHand", "CF2C84680E2AA55628912AC0BE3B6044EB9E0C9B1A713B611263F068334037FC")] // 공허의 손 원본과 그림 해시 대조
    [TestCase(189, "STARLIGHT_BRAND", "별빛 낙인", "StarlightBrand", "94EDE114344C801DB1C8E8E3780C7FDC7A7F9C3F5556FCCB416C78380A7C5B52")] // 별빛 낙인 원본과 그림 해시 대조
    [TestCase(190, "ILLUSION_SPLIT", "환영 분열", "IllusionSplit", "2F17FDF0F3501A68E308BAE1428A3BAC16592A90C4EE44D26365EA8BCE39A7F3")] // 환영 분열 원본과 그림 해시 대조
    [TestCase(191, "MANA_BACKFLOW", "마력 역류", "ManaBackflow", "584D88730DCB2D370CC534731F7084302DAB18FDAF99472E9021CD6FD70FF738")] // 마력 역류 원본과 그림 해시 대조
    [TestCase(192, "ARCANE_SURGE", "비전 쇄도", "ArcaneSurge", "7CBA7A4DBE1ACBF4D3ABD0BACEF8A7D07D98F741F32F5D030BB51D8133C854AD")] // 비전 쇄도 원본과 그림 해시 대조
    public void MatchesOriginalAndRestorableTreasure(int row, string id, string title, string assetName, string digest) // 카드 원화와 지급 설정 대조
    { // 원화 대조 시작
        VerifyTreasureArtwork(row, id, title, assetName, CardType.Wand, digest); // 시트 비용과 연결된 PNG 해시 확인
    } // 원화 대조 종료

    [UnityTest] // 실제 보물 버튼과 저장 복원 검사 등록
    public IEnumerator TreasuresRestoreTheirChosenWandOwners() // 신규 지팡이 카드의 두 가지 소유자 대조
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
        session.SetCurrentFloorSeed(265); // 저장 복원용 층 시드 설정
        var deck = RunDeckManager.EnsureInstance(); // 실제 획득용 회차 덱 조회
        deck.ResetToSource(PartyLoadoutRuntime.Active.Deck); // 출전 덱을 적용한 상태 준비
        var pending = new HashSet<string> // 획득해야 할 신규 카드 식별자 목록
        { // 신규 카드 목록 시작
            "CRD_SHEET_FIRE_DOWNPOUR", // 화염 폭우 카드 식별자
            "CRD_SHEET_THUNDER_PRISON", // 천둥 감옥 카드 식별자
            "CRD_SHEET_MANA_OVERLOAD", // 마력 폭주 카드 식별자
            "CRD_SHEET_VOID_HAND", // 공허의 손 카드 식별자
            "CRD_SHEET_STARLIGHT_BRAND", // 별빛 낙인 카드 식별자
            "CRD_SHEET_ILLUSION_SPLIT", // 환영 분열 카드 식별자
            "CRD_SHEET_MANA_BACKFLOW", // 마력 역류 카드 식별자
            "CRD_SHEET_ARCANE_SURGE" // 비전 쇄도 카드 식별자
        }; // 신규 카드 목록 종료
        var owners = new Dictionary<string, string>(); // 보상별 선택 소유자 정리
        var openedRooms = new List<string>(); // 처리한 실제 보물 방 기록
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 실제 보물 추첨 시드 함수 조회
        Assert.IsNotNull(hash); // 보물 시드 함수 확인
        var hashParameters = new object[1]; // 방 식별자 전달용 인수 배열
        string path = Path.Combine(Application.temporaryCachePath, "downpour-surge-wand-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로
        try // 검사 파일 정리 보장
        { // 보물 획득 순회 시작
            for (int index = 0; index < 12000 && pending.Count > 0; index++) // 한도 안에서 서로 다른 보물 방 조회
            { // 방별 추첨 시작
                string id = "winterfall-wand-265-" + index; // 보물 방의 고정 식별자
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
} // 지팡이 카드 검사 종료
