using System; // 기본 자료형과 예외 사용
using System.Collections.Generic; // 저장 목록 사용
using System.IO; // 저장 파일 입출력 사용
using UnityEngine; // 유니티 위치와 직렬화 사용

public static class ExplorationSaveService // 탐사 상태 저장 서비스
{ // 탐사 저장 서비스 시작
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "exploration-save.json"); // 기본 탐사 저장 경로
    [Serializable] // 탐사 상태 직렬화 지정
    private sealed class RunSnapshot // 탐사 회차 저장 정보
    { // 탐사 저장 정보 시작
        public int version; // 탐사 저장 형식 버전
        public int floor; // 현재 탐사 층
        public int seed; // 현재 층 생성 시드
        public Vector3 position; // 플레이어 위치
        public string[] party; // 출전 파티 순서
        public RunCard[] sourceCards; // 탐사 시작 덱 목록
        public RunCard[] cards; // 현재 회차 덱 목록
        public string[] cleared; // 클리어 조우 목록
        public string[] events; // 처리한 이벤트와 특수 방 목록
        public string[] relics; // 획득 순서 유물 목록
        public string[] consumables; // 소모품 슬롯 목록
        public string shopRoom; // 현재 상점 방 식별자
        public string[] purchasedOffers; // 현재 상점 판매 완료 상품 목록
        public bool restUsed; // 현재 휴식 방 사용 여부
        public int hazardHealth; // 대기 환경 체력 피해
        public int hazardMental; // 대기 환경 정신력 피해
        public float hazardExposure; // 현재 위험 노출도
        public int runExperience; // 탐사 획득 경험치 합계
        public int runGold; // 탐사 획득 골드 합계
        public int runScrew; // 탐사 획득 나사 합계
        public int runIronPlate; // 탐사 획득 철판 합계
        public int runWire; // 탐사 획득 전선 합계
        public string lobbyJson; // 동일 시점 거점 상태
    } // 탐사 저장 정보 종료
    [Serializable] // 회차 카드 직렬화 지정
    private sealed class RunCard // 카드 저장 정보
    { // 카드 저장 정보 시작
        public string card; // 카드 식별자
        public string owner; // 소유 캐릭터 식별자
        public int upgrade; // 카드 강화 단계
    } // 카드 저장 정보 종료
    public static bool Save(string path, Vector3 position) // 현재 탐사 상태 파일 저장
    { // 탐사 저장 시작
        string temporaryPath = null; // 임시 저장 경로 준비
        try // 저장 오류 처리 시작
        { // 저장 처리 시작
            ExplorationSessionManager session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
            BattleLoadoutData loadout = PartyLoadoutRuntime.Active; // 현재 탐사 편성 조회
            RunDeckManager deck = RunDeckManager.Instance; // 현재 회차 덱 조회
            if (string.IsNullOrWhiteSpace(path) || session == null || loadout == null || deck == null || !session.HasCurrentFloorSeed || session.IsExplorationCompleted || session.ActiveEncounter != null) // 정상 탐사 저장 조건 확인
            { // 저장 불가 처리 시작
                return false; // 기존 저장 유지
            } // 저장 불가 처리 종료
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) || float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) // 플레이어 위치 유효성 확인
            { // 잘못된 위치 처리 시작
                return false; // 기존 저장 유지
            } // 잘못된 위치 처리 종료
            if (!loadout.IsValidLoadout() || !LobbySaveService.TryCapture(out string lobbyJson)) // 탐사 편성과 거점 상태 확인
            { // 불완전 상태 처리 시작
                return false; // 기존 저장 유지
            } // 불완전 상태 처리 종료
            List<string> party = new List<string>(); // 파티 식별자 목록 생성
            foreach (CharacterData member in loadout.Party.Members) // 출전 캐릭터 순회
            { // 파티 수집 시작
                if (string.IsNullOrWhiteSpace(member.CharacterId)) // 파티 식별자 확인
                { // 잘못된 파티 처리 시작
                    return false; // 기존 저장 유지
                } // 잘못된 파티 처리 종료
                party.Add(member.CharacterId); // 파티 식별자 순서 보관
            } // 파티 수집 종료
            List<RunCard> sourceCards = new List<RunCard>(); // 탐사 시작 덱 목록 생성
            foreach (DeckCardEntry entry in loadout.Deck.Cards) // 탐사 시작 카드 순회
            { // 시작 카드 수집 시작
                if (string.IsNullOrWhiteSpace(entry.Card.CardId)) // 시작 카드 식별자 확인
                { // 잘못된 시작 카드 처리 시작
                    return false; // 기존 저장 유지
                } // 잘못된 시작 카드 처리 종료
                RunCard sourceCard = new RunCard // 탐사 시작 카드 정보 생성
                { // 시작 카드 정보 설정 시작
                    card = entry.Card.CardId, // 카드 식별자 설정
                    owner = entry.Owner.CharacterId, // 카드 소유자 설정
                    upgrade = 0 // 시작 강화 단계 설정
                }; // 시작 카드 정보 설정 종료
                sourceCards.Add(sourceCard); // 시작 카드와 소유자 기록
            } // 시작 카드 수집 종료
            List<RunCard> cards = new List<RunCard>(); // 회차 카드 목록 생성
            foreach (RunDeckCardEntry entry in deck.Cards) // 현재 회차 카드 순회
            { // 회차 카드 수집 시작
                if (entry == null || !entry.IsValid() || string.IsNullOrWhiteSpace(entry.Card.CardId)) // 카드 식별 정보 확인
                { // 잘못된 카드 처리 시작
                    return false; // 기존 저장 유지
                } // 잘못된 카드 처리 종료
                RunCard card = new RunCard // 현재 회차 카드 정보 생성
                { // 회차 카드 정보 설정 시작
                    card = entry.Card.CardId, // 카드 식별자 설정
                    owner = entry.Owner.CharacterId, // 카드 소유자 설정
                    upgrade = entry.UpgradeLevel // 카드 강화 단계 설정
                }; // 회차 카드 정보 설정 종료
                cards.Add(card); // 카드 소유자와 강화 기록
            } // 회차 카드 수집 종료
            List<string> cleared = new List<string>(session.ClearedEncounterIds); // 클리어 조우 목록 복사
            List<string> events = new List<string>(session.ResolvedEventIds); // 처리 이벤트 목록 복사
            cleared.Sort(StringComparer.Ordinal); // 조우 식별자 순서 정리
            events.Sort(StringComparer.Ordinal); // 이벤트 식별자 순서 정리
            List<string> relics = new List<string>(); // 보유 유물 목록 생성
            if (RelicRunManager.Instance != null) // 유물 관리자 존재 확인
            { // 유물 수집 시작
                foreach (RelicData relic in RelicRunManager.Instance.Inventory.OwnedRelics) // 획득 순서 유물 순회
                { // 유물 기록 시작
                    relics.Add(relic.RelicId); // 유물 식별자 보관
                } // 유물 기록 종료
            } // 유물 수집 종료
            string[] consumables = new string[ConsumableInventoryRuntime.SlotCount]; // 고정 소모품 슬롯 목록 생성
            if (ConsumableRunManager.Instance != null) // 소모품 관리자 존재 확인
            { // 소모품 수집 시작
                for (int index = 0; index < consumables.Length; index++) // 전체 소모품 슬롯 순회
                { // 슬롯 기록 시작
                    ConsumableItemData item = ConsumableRunManager.Instance.Inventory.GetItem(index); // 현재 슬롯 아이템 조회
                    consumables[index] = item != null ? item.ItemId : string.Empty; // 아이템 식별자 또는 빈 슬롯 기록
                } // 슬롯 기록 종료
            } // 소모품 수집 종료
            ShopRunManager shop = ShopRunManager.Instance; // 현재 상점 상태 조회
            List<string> purchasedOffers = new List<string>(); // 판매 완료 상품 목록 생성
            if (shop != null && shop.Catalog != null) // 상점 카탈로그 존재 확인
            { // 상점 수집 시작
                foreach (ShopOfferData offer in shop.Catalog.Offers) // 전체 상점 상품 순회
                { // 판매 상태 기록 시작
                    if (offer != null && shop.IsPurchased(offer)) // 판매 완료 상품 확인
                    { // 판매 완료 처리 시작
                        purchasedOffers.Add(offer.OfferId); // 판매 완료 식별자 기록
                    } // 판매 완료 처리 종료
                } // 판매 상태 기록 종료
            } // 상점 수집 종료
            ExplorationHazardRuntime hazard = UnityEngine.Object.FindFirstObjectByType<ExplorationHazardRuntime>(); // 현재 위험 상태 조회
            RunSnapshot snapshot = new RunSnapshot // 동일 시점 탐사 상태 생성
            { // 탐사 저장 정보 설정 시작
                version = 1, // 저장 형식 버전 설정
                floor = session.CurrentFloor, // 현재 탐사 층 설정
                seed = session.CurrentFloorSeed, // 현재 층 시드 설정
                position = position, // 플레이어 위치 설정
                party = party.ToArray(), // 출전 파티 설정
                sourceCards = sourceCards.ToArray(), // 탐사 시작 덱 설정
                cards = cards.ToArray(), // 현재 회차 덱 설정
                cleared = cleared.ToArray(), // 클리어 조우 설정
                events = events.ToArray(), // 처리 이벤트 설정
                relics = relics.ToArray(), // 보유 유물 설정
                consumables = consumables, // 소모품 슬롯 설정
                shopRoom = shop != null ? shop.ActiveRoomRuntimeId : null, // 상점 방 식별자 설정
                purchasedOffers = purchasedOffers.ToArray(), // 판매 완료 상품 설정
                restUsed = RestRoomRunManager.Instance != null && RestRoomRunManager.Instance.IsUsed, // 휴식 방 사용 상태 설정
                hazardHealth = session.PendingHazardHealthDamage, // 대기 체력 피해 설정
                hazardMental = session.PendingHazardMentalDamage, // 대기 정신력 피해 설정
                hazardExposure = hazard != null ? hazard.Exposure : 0f, // 위험 노출도 설정
                runExperience = session.RunExperienceGained, // 탐사 획득 경험치 설정
                runGold = session.RunGoldGained, // 탐사 획득 골드 설정
                runScrew = session.RunScrewGained, // 탐사 획득 나사 설정
                runIronPlate = session.RunIronPlateGained, // 탐사 획득 철판 설정
                runWire = session.RunWireGained, // 탐사 획득 전선 설정
                lobbyJson = lobbyJson // 동일 시점 거점 상태 설정
            }; // 탐사 저장 정보 설정 종료
            string fullPath = Path.GetFullPath(path); // 절대 저장 경로 계산
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); // 저장 폴더 준비
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp"; // 고유 임시 파일 경로 생성
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(snapshot)); // 임시 파일에 탐사 상태 기록
            if (File.Exists(fullPath)) // 기존 저장 파일 존재 확인
            { // 기존 저장 교체 시작
                File.Replace(temporaryPath, fullPath, null); // 기존 파일 원자적 교체
            } // 기존 저장 교체 종료
            else // 첫 저장 여부 확인
            { // 첫 저장 시작
                File.Move(temporaryPath, fullPath); // 임시 파일을 저장 파일로 이동
            } // 첫 저장 종료
            temporaryPath = null; // 임시 파일 이동 완료 표시
            return true; // 탐사 저장 성공 반환
        } // 저장 처리 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 경로와 입출력 오류 확인
        { // 저장 오류 시작
            Debug.LogWarning("[ExplorationSave] 저장 실패: " + exception.Message); // 저장 오류 안내
            return false; // 저장 실패 반환
        } // 저장 오류 종료
        finally // 임시 파일 정리 보장
        { // 임시 파일 정리 시작
            if (temporaryPath != null && File.Exists(temporaryPath)) // 남은 임시 파일 확인
            { // 남은 파일 정리 시작
                try // 임시 파일 제거 시도
                { // 임시 파일 제거 시작
                    File.Delete(temporaryPath); // 남은 임시 파일 제거
                } // 임시 파일 제거 종료
                catch (IOException) // 파일 오류 확인
                { // 파일 오류 처리 시작
                } // 파일 오류 처리 종료
                catch (UnauthorizedAccessException) // 권한 오류 확인
                { // 권한 오류 처리 시작
                } // 권한 오류 처리 종료
            } // 남은 파일 정리 종료
        } // 임시 파일 정리 종료
    } // 탐사 저장 종료
} // 탐사 저장 서비스 종료
