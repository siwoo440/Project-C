using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 보상 후보 목록 사용
using System.Reflection; // 보상 결정 검사 사용
using NUnit.Framework; // 보상 흐름 검사 사용
using UnityEngine; // 유니티 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 선택 버튼 검사 사용

public sealed class CommonCardTreasureTests // 공용 카드 보물 검사
{ // 검사 시작
    [UnityTest] // 실제 보물 선택 검사 지정
    public IEnumerator TreasureWaitsForOwnerAndRestoresChosenCard() // 선택 취소와 저장 복원 검사
    { // 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 경로 생성
        try // 독립 저장 파일 정리 보장
        { // 실제 탐사 검사 시작
            SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 확인
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 대기
            { // 거점 대기 시작
                yield return null; // 다음 프레임 대기
            } // 거점 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 거점 확인
            PartyDepartureTestSupport.StartExploration(0, 1); // 두 명 파티로 탐사 시작
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 대기
            { // 탐사 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 확인
            yield return null; // 상점 초기화 대기
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 조회
            Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 실제 카드 후보 준비
            MethodInfo candidates = typeof(ExplorationTreasureRewardService).GetMethod("GetValidOffers", BindingFlags.Static | BindingFlags.NonPublic); // 실제 카드 후보 조회
            MethodInfo hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 고정 보상 시드 조회
            List<ShopOfferData> offers = (List<ShopOfferData>)candidates.Invoke(null, new object[] { ShopOfferType.Card }); // 실제 카드 후보 계산
            Assert.IsTrue(offers.Exists(item => item.RequiresOwnerChoice)); // 공용 카드 보상 후보 확인
            string roomId = null; // 공용 카드 보물 식별자 준비
            ShopOfferData expected = null; // 예상 공용 카드 준비
            for (int index = 0; index < 10000 && roomId == null; index++) // 재현 가능한 보물 검색
            { // 보물 검색 시작
                string candidate = "common_card_reward_" + index; // 독립 보물 식별자 생성
                System.Random random = new System.Random((int)hash.Invoke(null, new object[] { candidate })); // 실제 고정 난수 생성
                int roll = random.Next(100); // 실제 보상 종류 추첨
                if (roll >= 20 && roll < 35) // 카드 보상 범위 확인
                { // 카드 추첨 확인 시작
                    ShopOfferData offer = offers[random.Next(offers.Count)]; // 실제 카드 후보 추첨
                    if (offer.RequiresOwnerChoice) // 공용 카드 선택 확인
                    { // 재현 보물 저장 시작
                        roomId = candidate; // 보물 식별자 저장
                        expected = offer; // 예상 카드 저장
                    } // 재현 보물 저장 종료
                } // 카드 추첨 확인 종료
            } // 보물 검색 종료
            Assert.IsNotNull(roomId); // 공용 카드 보물 검색 확인
            CharacterData owner = PartyLoadoutRuntime.Active.Party.Members[1]; // 선택할 파티원 조회
            int originalCount = RunDeckManager.Instance.CardCount; // 보상 전 카드 수 보관
            float originalTimeScale = Time.timeScale; // 보상 선택 전 시간 배율 보관
            GameObject roomObject = new GameObject("CommonCardTreasureTest"); // 독립 보물 방 생성
            ExplorationSpecialRoomView room = roomObject.AddComponent<ExplorationSpecialRoomView>(); // 실제 보물 동작 연결
            room.Initialize(null, Vector2Int.zero, ExplorationRoomType.Treasure, roomId); // 독립 보물 식별자 지정
            room.SendMessage("OpenTreasure"); // 실제 보물 열기
            Transform chooser = room.transform.Find("TreasureOwnerCanvas"); // 실제 소유자 선택 창 조회
            Assert.IsNotNull(chooser); // 소유자 선택 화면 확인
            Assert.AreEqual(0f, Time.timeScale); // 선택 중 탐사 일시 정지 확인
            Assert.IsFalse(ExplorationSessionManager.Instance.IsEventResolved(roomId)); // 선택 전 보물 미사용 확인
            Assert.AreEqual(originalCount, RunDeckManager.Instance.CardCount); // 선택 전 카드 미지급 확인
            chooser.Find("Panel/CancelButton").GetComponent<Button>().onClick.Invoke(); // 선택 취소
            yield return null; // 닫힌 창 정리 대기
            Assert.AreEqual(originalTimeScale, Time.timeScale); // 선택 취소 후 시간 배율 복원 확인
            Assert.IsFalse(ExplorationSessionManager.Instance.IsEventResolved(roomId)); // 취소 후 보물 유지 확인
            Assert.AreEqual(originalCount, RunDeckManager.Instance.CardCount); // 취소 후 카드 미지급 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 선택 전 독립 탐사 저장
            room.SendMessage("OpenTreasure"); // 동일 보물 다시 열기
            chooser = room.transform.Find("TreasureOwnerCanvas"); // 재개한 선택 창 조회
            Assert.IsNotNull(chooser); // 재선택 화면 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 선택 중 이전 저장 복원
            Assert.AreEqual(originalTimeScale, Time.timeScale); // 저장 복원 후 시간 배율 복원 확인
            yield return null; // 복원으로 닫힌 선택 창 정리 대기
            Assert.IsNull(room.transform.Find("TreasureOwnerCanvas")); // 저장 복원 시 선택 화면 종료 확인
            Assert.IsFalse(ExplorationSessionManager.Instance.IsEventResolved(roomId)); // 복원 후 보물 미사용 확인
            Assert.AreEqual(originalCount, RunDeckManager.Instance.CardCount); // 복원 후 보상 미지급 확인
            room.SendMessage("OpenTreasure"); // 저장 복원 후 동일 보물 열기
            chooser = room.transform.Find("TreasureOwnerCanvas"); // 복원 후 선택 창 조회
            Assert.IsNotNull(chooser); // 복원 후 선택 화면 확인
            chooser.Find("Panel/Owner_" + owner.CharacterId).GetComponent<Button>().onClick.Invoke(); // 두 번째 파티원 선택
            Assert.AreEqual(originalTimeScale, Time.timeScale); // 카드 지급 후 시간 배율 복원 확인
            Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(roomId)); // 지급 후 보물 사용 확인
            Assert.AreEqual(originalCount + 1, RunDeckManager.Instance.CardCount); // 카드 한 장 지급 확인
            RunDeckCardEntry added = RunDeckManager.Instance.Cards[originalCount]; // 지급 카드 조회
            Assert.AreSame(expected.Card, added.Card); // 동일 보상 카드 확인
            Assert.AreSame(owner, added.Owner); // 직접 선택한 소유자 확인
            typeof(ExplorationSpecialRoomView).GetMethod("OpenTreasure", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(room, null); // 비활성 보물의 중복 열기 시도
            Assert.AreEqual(originalCount + 1, RunDeckManager.Instance.CardCount); // 중복 카드 지급 방지 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 독립 탐사 저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 독립 탐사 복원 확인
            Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(roomId)); // 보물 사용 상태 복원 확인
            Assert.AreEqual(expected.Card.CardId, RunDeckManager.Instance.Cards[originalCount].Card.CardId); // 보상 카드 복원 확인
            Assert.AreEqual(owner.CharacterId, RunDeckManager.Instance.Cards[originalCount].Owner.CharacterId); // 보상 소유자 복원 확인
            Object.Destroy(roomObject); // 독립 보물 방 제거
        } // 실제 탐사 검사 종료
        finally // 독립 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 검사 본문 종료
} // 검사 종료
