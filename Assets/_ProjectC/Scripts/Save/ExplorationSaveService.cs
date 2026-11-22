using System; // 기본 자료형과 예외 사용
using System.Collections.Generic; // 저장 목록 사용
using System.IO; // 저장 파일 입출력 사용
using UnityEngine; // 유니티 위치와 직렬화 사용

public static class ExplorationSaveService // 탐사 상태 저장 서비스
{ // 탐사 저장 서비스 시작
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "exploration-save.json"); // 기본 탐사 저장 경로
    [Serializable] // 탐사 상태 직렬화 지정
    internal sealed class RunSnapshot // 탐사 회차 저장 정보
    { // 탐사 저장 정보 시작
        public int version; // 탐사 저장 형식 버전
        public int zoneVersion; // 없는 이전 저장에서는 영인 출발 구역 형식
        public int zone; // 저장 시점에 진행 중인 구역 번호
        public int zoneLevel; // 저장 시점에 고정된 출전 난이도
        public bool zoneBossReserved; // 출전 시점에 확정한 관문 보스 예약
        public int encounterPoolVersion; // 탐사 중 고정된 조우 목록 버전
        public int eventPoolVersion; // 현재 층 이벤트 목록 버전과 이전 저장의 기본값 영
        public int floor; // 현재 탐사 층
        public int seed; // 현재 층 생성 시드
        public bool hasOptionalBossRoom; // 선택 보스 방 존재와 구버전 기본값 거짓
        public Vector2Int optionalBossRoom; // 현재 층에서 확정한 선택 보스 좌표
        public Vector3 position; // 플레이어 위치
        public string[] party; // 출전 파티 순서
        public RunCard[] sourceCards; // 탐사 시작 덱 목록
        public RunCard[] cards; // 현재 회차 덱 목록
        public string[] cleared; // 클리어 조우 목록
        public string[] events; // 처리한 이벤트와 특수 방 목록
        public string pendingMapFragment; // 사용 대기 중인 지도 조각의 원본 이벤트
        public bool usedSealedMap; // 이번 탐사의 봉인된 지도 사용 여부
        public GlassFieldEffect glassFieldEffects; // 다음 교전의 회피와 실명 보관
        public WandererCryRunState wandererCry; // 교전 전 추적과 세 카드 및 귀 막기 보관
        public CrackedCompassRunState compass; // 첫 일반 방 사용 여부와 발견한 사건
        public string[] avoidedEncounters; // 승리 보상 없이 회피한 조우 목록
        public EscapeGoldClaim[] escapeGoldClaims; // 조우별 선지급 도주 골드 목록
        public CharredWellRunState well; // 사용자별 물과 현재 및 다음 층 봉인 저장
        public BurntCartRunState cart; // 수레 매복과 상점 할인 저장
        public string[] relics; // 획득 순서 유물 목록
        public int permanentAttackPower; // 유물을 제거한 뒤에도 보존하는 회차 영구 공격력
        public int pendingCaravanDraws; // 이전 파일에서 생략 가능하고 탐사에 유지하는 저울의 예약 드로우
        public string[] consumables; // 소모품 슬롯 목록
        public string shopRoom; // 현재 상점 방 식별자
        public ShopRunManager.RelicReserve[] shopRelicReserves; // 방별 유물과 특수 카드의 추가 진열 및 판매 결과
        public bool shopDrawerDiscountUsed; // 금속 손잡이 할인 사용 여부
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
        public bool hasResourceLedger; // 탐사 획득 재화 구분 저장 여부
        public int pendingSpecimenDraw; // 다음 전투에서 지급할 괴생체 승리의 드로우
        public int pendingSpecimenActionPoints; // 다음 전투에서 지급할 괴생체 승리의 행동력
        public int temporaryGold; // 남은 탐사 획득 골드
        public int temporaryScrew; // 남은 탐사 획득 나사
        public int temporaryIronPlate; // 남은 탐사 획득 철판
        public int temporaryWire; // 남은 탐사 획득 전선
        public string lobbyJson; // 동일 시점 거점 상태
    } // 탐사 저장 정보 종료
    [Serializable] // 회차 카드 직렬화 지정
    internal sealed class RunCard // 카드 저장 정보
    { // 카드 저장 정보 시작
        public string card; // 카드 식별자
        public string owner; // 소유 캐릭터 식별자
        public int upgrade; // 카드 강화 단계
    } // 카드 저장 정보 종료
    [Serializable] // 조우별 도주 골드 직렬화 지정
    internal sealed class EscapeGoldClaim // 도주 골드 지급 정보
    { // 지급 정보 시작
        public string encounter; // 해당 런타임 조우 식별자
        public int gold; // 이미 지갑으로 지급한 골드
    } // 지급 정보 종료
    private sealed class ResolvedRun // 저장 식별자 검증 결과
    { // 검증 결과 시작
        public RunSnapshot snapshot; // 검증된 탐사 저장 정보
        public List<CharacterData> party; // 복원할 파티 원본
        public List<DeckCardEntry> sourceCards; // 복원할 시작 덱 원본
        public List<RunDeckCardEntry> cards; // 복원할 현재 회차 덱
        public List<RelicData> relics; // 복원할 유물 원본
        public ConsumableItemData[] consumables; // 복원할 소모품 슬롯
        public ShopCatalogData shopCatalog; // 복원할 상점 원본
    } // 검증 결과 종료
    public static bool Save(string path, Vector3 position) // 현재 탐사 상태 파일 저장
    { // 일반 탐사 저장 시작
        return Save(path, position, null); // 보유 유물을 그대로 기록
    } // 일반 탐사 저장 종료
    internal static bool Save(string path, Vector3 position, RelicData consumedMap) // 지도 이동 성공 상태와 소모할 유물 함께 저장
    { // 탐사 저장 시작
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
            var avoided = new List<string>(session.AvoidedEncounterIds); // 지급과 별도로 관리하는 회피 조우 복사
            avoided.Sort(StringComparer.Ordinal); // 회피 조우 저장 순서 고정
            var escapeClaims = new List<EscapeGoldClaim>(); // 저장할 도주 골드 목록 초기화
            foreach (var pair in session.EscapeGoldClaims) // 런타임 조우별 선지급 보상 순회
            { // 개별 도주 골드 기록 시작
                var claim = new EscapeGoldClaim // 조우의 도주 골드 저장 항목 생성
                { // 지급 정보 기록 시작
                    encounter = pair.Key, // 런타임 조우 식별자 복사
                    gold = pair.Value // 입금된 골드 복사
                }; // 지급 정보 기록 종료
                escapeClaims.Add(claim); // 실제 지급 정보 목록에 추가
            } // 개별 도주 골드 기록 종료
            escapeClaims.Sort((left, right) => StringComparer.Ordinal.Compare(left.encounter, right.encounter)); // 도주 골드 저장 순서 고정
            List<string> relics = new List<string>(); // 보유 유물 목록 생성
            if (RelicRunManager.Instance != null) // 유물 관리자 존재 확인
            { // 유물 수집 시작
                foreach (RelicData relic in RelicRunManager.Instance.Inventory.OwnedRelics) // 획득 순서 유물 순회
                { // 유물 기록 시작
                    if (relic != consumedMap) // 이동과 함께 소모하는 지도 확인
                    { // 남은 유물 기록 시작
                        relics.Add(relic.RelicId); // 나머지 유물의 보유 순서 보존
                    } // 남은 유물 기록 종료
                } // 유물 기록 종료
            } // 유물 수집 종료
            string[] consumables = new string[ConsumableRunManager.Instance?.Inventory.Capacity ?? ConsumableInventoryRuntime.SlotCount]; // 실제 보관칸 수를 포함한 저장 목록
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
                zoneVersion = 1, // 현재 구역 선택 필드의 옵션 형식 기록
                zone = (int)session.CurrentZone, // 진행 중인 실제 탐사 구역 저장
                zoneLevel = session.CurrentZoneLevel, // 현재 탐사의 실제 출전 레벨 저장
                zoneBossReserved = session.IsZoneBossReservedForRun, // 출전 때 고정한 관문 보스 예약 저장
                encounterPoolVersion = session.EncounterPoolVersion, // 현재 탐사의 고정 조우 목록 버전 저장
                floor = session.CurrentFloor, // 현재 탐사 층 설정
                seed = session.CurrentFloorSeed, // 현재 층 시드 설정
                hasOptionalBossRoom = session.CurrentFloorOptionalBossRoom.HasValue, // 현재 층에서 선택 보스 판정 성공 여부
                optionalBossRoom = session.CurrentFloorOptionalBossRoom.GetValueOrDefault(), // 생성 시 확정한 선택 방 좌표
                eventPoolVersion = session.EventPoolVersion, // 현재 층 이벤트 구성 저장
                position = position, // 플레이어 위치 설정
                party = party.ToArray(), // 출전 파티 설정
                sourceCards = sourceCards.ToArray(), // 탐사 시작 덱 설정
                cards = cards.ToArray(), // 현재 회차 덱 설정
                cleared = cleared.ToArray(), // 클리어 조우 설정
                events = events.ToArray(), // 처리 이벤트 설정
                pendingMapFragment = session.PendingMapFragmentId, // 보관 중인 일회성 지도 조각 저장
                usedSealedMap = consumedMap != null || session.UsedSealedMap, // 이동과 소모를 같은 저장에 반영
                well = session.Well, // 우물 회복과 봉인 판정 기록
                cart = session.Cart, // 수레가 남긴 매복과 미사용 할인 기록
                glassFieldEffects = session.PendingGlassFieldEffects, // 들판 선택의 미사용 전투 효과 저장
                wandererCry = session.WandererCry, // 선택한 방과 확정 카드 후보를 함께 저장
                compass = session.Compass, // 출발 기회와 현재 층 사건 원본 저장
                avoidedEncounters = avoided.ToArray(), // 보상 없이 통과한 조우 저장
                escapeGoldClaims = escapeClaims.ToArray(), // 재도주 방지와 승리 차감에 쓸 실제 지급량 저장
                relics = relics.ToArray(), // 보유 유물 설정
                permanentAttackPower = session.PermanentAttackPower, // 유물 목록과 독립된 실제 영구 공격력 기록
                pendingCaravanDraws = session.PendingCaravanDraws, // 실제 구매로 예약하고 아직 받지 않은 카드 수 저장
                consumables = consumables, // 소모품 슬롯 설정
                shopRoom = shop != null ? shop.ActiveRoomRuntimeId : null, // 상점 방 식별자 설정
                shopRelicReserves = shop?.ExportRelicReserves(), // 열쇠와 도장의 방별 추가 진열 및 판매 결과 보관
                shopDrawerDiscountUsed = shop?.DrawerDiscountUsed ?? false, // 실제 결제로 소비한 첫 상품 할인 보관
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
                pendingSpecimenDraw = session.PendingSpecimenDraw, // 다음 전투용 미사용 드로우 저장
                pendingSpecimenActionPoints = session.PendingSpecimenActionPoints, // 다음 전투용 미사용 행동력 저장
                hasResourceLedger = PlayerResourceManager.EnsureInstance().IsTrackingExplorationResources, // 탐사 재화 구분 저장 여부
                temporaryGold = PlayerResourceManager.EnsureInstance().ExplorationGold, // 남은 탐사 골드 저장
                temporaryScrew = PlayerResourceManager.EnsureInstance().ExplorationScrew, // 남은 탐사 나사 저장
                temporaryIronPlate = PlayerResourceManager.EnsureInstance().ExplorationIronPlate, // 남은 탐사 철판 저장
                temporaryWire = PlayerResourceManager.EnsureInstance().ExplorationWire, // 남은 탐사 전선 저장
                lobbyJson = lobbyJson // 동일 시점 거점 상태 설정
            }; // 탐사 저장 정보 설정 종료
            return SaveFileStorage.Write(path, JsonUtility.ToJson(snapshot)); // 이전 정상본을 보존하며 탐사 저장
        } // 저장 처리 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 경로와 입출력 오류 확인
        { // 저장 오류 시작
            Debug.LogWarning("[ExplorationSave] 저장 실패: " + exception.Message); // 저장 오류 안내
            return false; // 저장 실패 반환
        } // 저장 오류 종료
    } // 탐사 저장 종료
    public static bool Remove(string path) // 이전 탐사 저장 제거
    { // 탐사 저장 제거 시작
        try // 파일 오류 처리 시작
        { // 파일 제거 시작
            if (string.IsNullOrWhiteSpace(path)) // 저장 경로 확인
            { // 경로 누락 처리 시작
                return false; // 제거 실패 반환
            } // 경로 누락 처리 종료
            string backup = SaveFileStorage.BackupPath(path); // 이전 정상 탐사 저장 경로 조회
            if (File.Exists(backup)) // 이전 정상 탐사 저장 존재 확인
            { // 이전 정상 저장 정리 시작
                File.Delete(backup); // 복구본 재생성 방지를 위한 이전 저장 삭제
            } // 이전 정상 저장 정리 종료
            if (File.Exists(path)) // 탐사 저장 파일 존재 확인
            { // 기존 저장 제거 시작
                File.Delete(path); // 지정 탐사 저장 파일 제거
            } // 기존 저장 제거 종료
            return true; // 저장 제거 성공 반환
        } // 파일 제거 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 경로와 권한 오류 확인
        { // 저장 제거 오류 시작
            Debug.LogWarning("[ExplorationSave] 이전 저장 제거 실패: " + exception.Message); // 저장 제거 오류 안내
            return false; // 제거 실패 반환
        } // 저장 제거 오류 종료
    } // 탐사 저장 제거 종료
    public static bool CanLoad(string path) // 탐사 저장 복원 가능 여부 확인
    { // 탐사 저장 검사 시작
        return TryRead(path, out ResolvedRun ignored); // 전체 저장 검증 결과 반환
    } // 탐사 저장 검사 종료
    public static bool CanLoadJson(string json) // 탐사 저장 내용의 복원 가능 여부 확인
    { // 탐사 저장 내용 검사 시작
        try // 저장 형식 오류 처리
        { // 탐사 저장 내용 분석 시작
            RunSnapshot snapshot = JsonUtility.FromJson<RunSnapshot>(json); // 탐사 저장 내용 역직렬화
            return TryResolve(snapshot, out ResolvedRun ignored); // 실제 원본 기준 전체 탐사 상태 검증
        } // 탐사 저장 내용 분석 종료
        catch (ArgumentException) // 잘못된 저장 형식 확인
        { // 저장 형식 오류 처리 시작
            return false; // 복원 불가 반환
        } // 저장 형식 오류 처리 종료
    } // 탐사 저장 내용 검사 종료
    public static bool Load(string path) // 저장된 탐사 상태 복원
    { // 탐사 복원 시작
        if (!TryRead(path, out ResolvedRun run)) // 저장 파일 전체 검증
        { // 잘못된 저장 처리 시작
            return false; // 기존 진행 상태 유지
        } // 잘못된 저장 처리 종료
        PartyData party = PartyData.CreateRuntime(run.party); // 저장 파티 원본 생성
        DeckData sourceDeck = DeckData.CreateRuntime(run.sourceCards); // 저장 시작 덱 생성
        BattleLoadoutData loadout = BattleLoadoutData.CreateRuntime(party, sourceDeck); // 복원 탐사 편성 생성
        if (!LobbySaveService.LoadJson(run.snapshot.lobbyJson)) // 저장된 거점 상태 복원
        { // 거점 상태 복원 실패 시작
            UnityEngine.Object.Destroy(loadout); // 임시 편성 제거
            UnityEngine.Object.Destroy(sourceDeck); // 임시 시작 덱 제거
            UnityEngine.Object.Destroy(party); // 임시 파티 제거
            return false; // 탐사 복원 실패 반환
        } // 거점 상태 복원 실패 종료
        ExplorationRewardOwnerView.ClosePending(); // 복원 전 대기 중 보물 선택 종료
        ExplorationEventPanelView.CloseForLoad(); // 복원 전 이전 이벤트 선택과 이동 잠금 해제
        ShopRunManager.Instance?.ResetSession(); // 이전 상점 상태 제거
        RestRoomRunManager.Instance?.ResetSession(); // 이전 휴식 상태 제거
        RunDeckManager deck = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        deck.ResetToSource(null); // 이전 회차 덱 연결 해제
        PartyLoadoutRuntime.Restore(loadout); // 저장 탐사 편성 연결
        deck.RestoreCards(sourceDeck, run.cards); // 저장 회차 덱 연결
        BattleResultManager.EnsureInstance().RegisterParty(party); // 저장 파티 상태 등록
        ConsumableInventoryRuntime inventory = ConsumableRunManager.EnsureInstance().Inventory; // 복원할 회차의 보관함 준비
        inventory.Clear(); // 이전 포션을 지운 뒤 이전 보관대 반납
        RelicRunManager relicManager = RelicRunManager.EnsureInstance(); // 유물 관리자 준비
        while (relicManager.Inventory.Count > 0) // 이전 유물 존재 확인
        { // 이전 유물 제거 시작
            relicManager.TryRemove(relicManager.Inventory.OwnedRelics[0].RelicId); // 첫 이전 유물 제거
        } // 이전 유물 제거 종료
        foreach (RelicData relic in run.relics) // 저장 유물 순회
        { // 유물 복원 시작
            relicManager.TryAcquire(relic); // 저장 순서 유물 복원
        } // 유물 복원 종료
        for (int index = 0; index < run.consumables.Length; index++) // 저장 소모품 슬롯 순회
        { // 소모품 슬롯 복원 시작
            if (run.consumables[index] != null) // 저장 소모품 존재 확인
            { // 소모품 복원 시작
                inventory.TrySetForDebug(index, run.consumables[index]); // 원래 슬롯에 소모품 배치
            } // 소모품 복원 종료
        } // 소모품 슬롯 복원 종료
        ExplorationSessionManager.EnsureInstance().RestoreRunState(run.snapshot); // 저장 층과 조우 상태 복원
        PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 저장 자원 관리자 조회
        if (run.snapshot.hasResourceLedger) // 획득 재화 구분 저장 여부 확인
        { // 탐사 획득 재화 복원 시작
            resources.RestoreExplorationResourceTracking(run.snapshot.temporaryGold, run.snapshot.temporaryScrew, run.snapshot.temporaryIronPlate, run.snapshot.temporaryWire); // 검증된 탐사 획득분 복원
        } // 탐사 획득 재화 복원 종료
        else // 이전 저장의 획득 구분 누락 확인
        { // 이전 저장 호환 시작
            resources.BeginExplorationResourceTracking(); // 기존 잔액을 보존하고 이후 획득분부터 구분
        } // 이전 저장 호환 종료
        ShopRunManager.EnsureInstance().RestoreSessionWithShopStock(run.shopCatalog, sourceDeck, run.snapshot.shopRoom, run.snapshot.purchasedOffers, run.snapshot.shopRelicReserves, run.snapshot.shopDrawerDiscountUsed); // 저장 상점과 첫 상품 구매 할인 상태 복원
        RestRoomRunManager.EnsureInstance().RestoreSession(sourceDeck, run.snapshot.restUsed); // 저장 휴식 상태 복원
        return true; // 탐사 복원 성공 반환
    } // 탐사 복원 종료
    private static bool TryRead(string path, out ResolvedRun run) // 탐사 저장 파일 읽기와 검증
    { // 탐사 저장 읽기 시작
        run = null; // 실패 기본값 설정
        try // 파일 오류 처리 시작
        { // 파일 읽기 시작
            if (!SaveFileStorage.TryRead(path, CanLoadJson, out string json, out string actualPath)) // 정상 탐사 원본 또는 복구본 확인
            { // 저장 파일 없음 처리 시작
                return false; // 복원 불가 반환
            } // 저장 파일 없음 처리 종료
            RunSnapshot snapshot = JsonUtility.FromJson<RunSnapshot>(json); // 검증된 탐사 저장 내용 역직렬화
            return TryResolve(snapshot, out run); // 전체 저장 검증 결과 반환
        } // 파일 읽기 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 또는 저장 형식 오류 확인
        { // 저장 읽기 오류 시작
            return false; // 복원 불가 반환
        } // 저장 읽기 오류 종료
    } // 탐사 저장 읽기 종료
    private static bool TryResolve(RunSnapshot snapshot, out ResolvedRun run) // 저장 식별자와 값 검증
    { // 저장 검증 시작
        run = null; // 실패 기본값 설정
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 원본 목록 조회
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 원본 목록 조회
        ShopCatalogData shopCatalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 상점 원본 목록 조회
        if (snapshot == null || snapshot.version != 1 || roster == null || catalog == null || shopCatalog == null || !shopCatalog.IsValidData() || !LobbySaveService.CanLoadJson(snapshot.lobbyJson)) // 필수 저장 정보 확인
        { // 필수 정보 오류 시작
            return false; // 복원 불가 반환
        } // 필수 정보 오류 종료
        if (!ExpeditionZoneProgress.IsValidRunSelection(snapshot.zoneVersion, snapshot.zone, snapshot.zoneLevel, snapshot.zoneBossReserved)) // 이전 또는 현재 출발 구역의 옵션 저장 유효성 확인
        { // 미지원 출전 정보의 복원 제외 시작
            return false; // 다른 기록이나 이전 정상본이 없는 잘못된 출발 방지
        } // 미지원 출전 정보의 복원 제외 종료
        if (snapshot.encounterPoolVersion < 0 || snapshot.encounterPoolVersion > ExplorationSessionManager.LatestEncounterPoolVersion) // 지원 조우 목록 버전 확인
        { // 미지원 목록 버전 처리 시작
            return false; // 알 수 없는 조우 구성의 복원 차단
        } // 미지원 목록 버전 처리 종료
        if (snapshot.wandererCry != null && !snapshot.wandererCry.IsValid(snapshot.events)) // 기존 체크포인트의 방랑자 출처와 후보 검사
        { // 잘못된 보상 예약 처리 시작
            return false; // 중복 후보나 출처 없는 추적 복원 차단
        } // 잘못된 보상 예약 처리 종료
        if ((snapshot.glassFieldEffects & ~(GlassFieldEffect.Evasion | GlassFieldEffect.Blind)) != 0) // 지원하지 않는 들판 효과 확인
        { // 잘못된 효과 처리 시작
            return false; // 알 수 없는 효과 복원 차단
        } // 잘못된 효과 처리 종료
        if (snapshot.eventPoolVersion < 0 || snapshot.eventPoolVersion > ExplorationEventCatalog.CurrentVersion) // 지원 이벤트 목록 버전 확인
        { // 잘못된 이벤트 버전 처리 시작
            return false; // 복원할 수 없는 이벤트 구성 차단
        } // 잘못된 이벤트 버전 처리 종료
        var savedZone = snapshot.zoneVersion == 0 ? ExpeditionZoneId.WinterfallAshPlains : (ExpeditionZoneId)snapshot.zone; // 이전 저장 또는 현재 유효 구역의 사건 목록 선택
        if (snapshot.compass != null && !snapshot.compass.IsValid(snapshot.floor, snapshot.eventPoolVersion, savedZone)) // 발견 정보와 저장된 지역 사건 목록 확인
        { // 나침반 상태 오류 시작
            return false; // 출처 없는 발견 사건의 복원 방지
        } // 나침반 상태 오류 종료
        if (snapshot.floor < 1 || snapshot.floor > ExplorationFloorRules.TotalFloorCount || !IsFinite(snapshot.position.x) || !IsFinite(snapshot.position.y) || !IsFinite(snapshot.position.z) || !IsFinite(snapshot.hazardExposure) || snapshot.hazardExposure < 0f || snapshot.hazardExposure >= 100f) // 층과 위치 및 위험 노출도 검사
        { // 위치 정보 오류 시작
            return false; // 복원 불가 반환
        } // 위치 정보 오류 종료
        if (snapshot.hazardHealth < 0 || snapshot.hazardMental < 0 || snapshot.runExperience < 0 || snapshot.runGold < 0 || snapshot.runScrew < 0 || snapshot.runIronPlate < 0 || snapshot.runWire < 0) // 탐사 합계와 피해 범위 검사
        { // 탐사 수치 오류 시작
            return false; // 복원 불가 반환
        } // 탐사 수치 오류 종료
        if (snapshot.pendingSpecimenDraw < 0 || snapshot.pendingSpecimenActionPoints < 0) // 저장된 시작 보상의 음수 여부 확인
        { // 잘못된 예약 처리 시작
            return false; // 올바르지 않은 표본병 예약 복원 차단
        } // 잘못된 예약 처리 종료
        if (snapshot.hasResourceLedger && !LobbySaveService.ContainsResources(snapshot.lobbyJson, snapshot.temporaryGold, snapshot.temporaryScrew, snapshot.temporaryIronPlate, snapshot.temporaryWire)) // 탐사 획득분과 저장 전체 재화 비교
        { // 잘못된 탐사 재화 처리 시작
            return false; // 전체 잔액을 초과하는 획득분 복원 차단
        } // 잘못된 탐사 재화 처리 종료
        if (snapshot.party == null || snapshot.party.Length < 1 || snapshot.party.Length > PartyData.MaxPartySize || snapshot.sourceCards == null || snapshot.sourceCards.Length < 1 || snapshot.cards == null || snapshot.relics == null || snapshot.consumables == null || (snapshot.consumables.Length < ConsumableInventoryRuntime.SlotCount || snapshot.consumables.Length > ConsumableInventoryRuntime.MaximumSlotCount) || !ValidIds(snapshot.cleared) || !ValidIds(snapshot.events) || !ValidIds(snapshot.purchasedOffers)) // 필수 목록과 식별자 검사
        { // 목록 오류 시작
            return false; // 복원 불가 반환
        } // 목록 오류 종료
        if (!ValidSpecialStock(snapshot.shopRelicReserves, shopCatalog)) // 방에 추가한 특수 판매 상품의 실제 원본과 식별자 확인
        { // 특수 상품 저장 형식 실패 시작
            return false; // 일치하는 특수 카드 재고를 복원할 수 없는 저장 거부
        } // 특수 상품 저장 형식 실패 종료
        if (string.IsNullOrEmpty(snapshot.shopRoom) && snapshot.purchasedOffers.Length > 0) // 상점 방 없는 판매 기록 확인
        { // 상점 기록 오류 시작
            return false; // 복원 불가 반환
        } // 상점 기록 오류 종료
        if (snapshot.cart != null && !snapshot.cart.IsValid(snapshot.events, snapshot.cleared, snapshot.encounterPoolVersion)) // 수레 원본과 미사용 상태 확인
        { // 손상된 수레 저장 처리 시작
            return false; // 잘못된 매복과 할인 복원 차단
        } // 손상된 수레 저장 처리 종료
        if (snapshot.well != null && !snapshot.well.IsValid(snapshot.party, snapshot.events)) // 행동자와 우물 봉인 원본 유효성 확인
        { // 손상된 우물 기록 처리 시작
            return false; // 잘못된 효과 보유자와 완료 방 복원 차단
        } // 손상된 우물 기록 처리 종료
        if (snapshot.hasOptionalBossRoom && ExplorationFloorRules.IsBossFloor(snapshot.floor)) // 필수 보스 층에 선택 보스가 섞인 저장 확인
        { // 선택 보스층 불일치 처리 시작
            return false; // 잘못된 층 배치 복원 차단
        } // 선택 보스층 불일치 처리 종료
        if (!string.IsNullOrEmpty(snapshot.pendingMapFragment) && Array.IndexOf(snapshot.events, snapshot.pendingMapFragment) < 0) // 조각을 준 이벤트의 완료 기록 확인
        { // 잘못된 조각 복원 차단 시작
            return false; // 원본 방이 없는 보관 조각 차단
        } // 잘못된 조각 복원 차단 종료
        if (snapshot.avoidedEncounters != null && (!ValidIds(snapshot.avoidedEncounters) || Array.Exists(snapshot.avoidedEncounters, id => Array.IndexOf(snapshot.cleared, id) >= 0))) // 중복 회피와 승리 기록 충돌 확인
        { // 잘못된 회피 기록 처리 시작
            return false; // 회피와 승리가 겹치는 저장 차단
        } // 잘못된 회피 기록 처리 종료
        if (snapshot.escapeGoldClaims != null && !ValidEscapeGoldClaims(snapshot)) // 신규 지급 기록이 있는 저장의 유효성 확인
        { // 잘못된 도주 골드 기록 처리 시작
            return false; // 중복 지급이나 승리 조우의 도주 기록 복원 차단
        } // 잘못된 도주 골드 기록 처리 종료
        foreach (string offerId in snapshot.purchasedOffers) // 판매 완료 상품 순회
        { // 상품 식별자 검사 시작
            if (!shopCatalog.ContainsOffer(offerId)) // 상점 상품 식별자 확인
            { // 알 수 없는 상품 처리 시작
                return false; // 복원 불가 반환
            } // 알 수 없는 상품 처리 종료
        } // 상품 식별자 검사 종료
        List<CharacterData> party = new List<CharacterData>(); // 복원 파티 목록 생성
        HashSet<string> partyIds = new HashSet<string>(); // 파티 식별자 중복 검사 목록
        foreach (string id in snapshot.party) // 저장 파티 순회
        { // 파티 식별자 검사 시작
            CharacterData character = FindUnique(roster.Characters, id, item => item.CharacterId); // 파티 캐릭터 원본 조회
            if (character == null || !partyIds.Add(id)) // 캐릭터 식별자와 중복 확인
            { // 잘못된 파티 처리 시작
                return false; // 복원 불가 반환
            } // 잘못된 파티 처리 종료
            party.Add(character); // 저장 순서 파티원 추가
        } // 파티 식별자 검사 종료
        List<DeckCardEntry> sourceCards = new List<DeckCardEntry>(); // 복원 시작 덱 목록 생성
        foreach (RunCard savedCard in snapshot.sourceCards) // 저장 시작 카드 순회
        { // 시작 카드 검사 시작
            if (!TryResolveCard(savedCard, roster, catalog, partyIds, out RunDeckCardEntry card) || savedCard.upgrade != 0) // 카드 원본과 시작 강화 단계 확인
            { // 잘못된 시작 카드 처리 시작
                return false; // 복원 불가 반환
            } // 잘못된 시작 카드 처리 종료
            sourceCards.Add(new DeckCardEntry(card.Card, card.Owner)); // 시작 덱 원본 추가
        } // 시작 카드 검사 종료
        List<RunDeckCardEntry> cards = new List<RunDeckCardEntry>(); // 복원 회차 덱 목록 생성
        foreach (RunCard savedCard in snapshot.cards) // 저장 회차 카드 순회
        { // 회차 카드 검사 시작
            if (!TryResolveCard(savedCard, roster, catalog, partyIds, out RunDeckCardEntry card)) // 카드 원본과 강화 단계 확인
            { // 잘못된 회차 카드 처리 시작
                return false; // 복원 불가 반환
            } // 잘못된 회차 카드 처리 종료
            cards.Add(card); // 저장 순서 회차 카드 추가
        } // 회차 카드 검사 종료
        List<RelicData> relics = new List<RelicData>(); // 복원 유물 목록 생성
        HashSet<string> relicIds = new HashSet<string>(); // 유물 중복 검사 목록
        foreach (string id in snapshot.relics) // 저장 유물 순회
        { // 유물 검사 시작
            RelicData relic = FindUnique(catalog.Relics, id, item => item.RelicId); // 유물 원본 조회
            if (relic == null || !relic.IsValidData() || !relicIds.Add(id)) // 유물 식별자와 중복 확인
            { // 잘못된 유물 처리 시작
                return false; // 복원 불가 반환
            } // 잘못된 유물 처리 종료
            relics.Add(relic); // 획득 순서 유물 추가
        } // 유물 검사 종료
        bool hasRack = relics.Exists(relic => relic.EffectType == RelicEffectType.ExtraPotionSlot); // 저장된 유물 원본의 보관대 확인
        if (snapshot.consumables.Length > ConsumableInventoryRuntime.SlotCount && !hasRack) // 보관대 없이 추가칸을 기록한 저장 검사
        { // 무효 저장 처리 시작
            return false; // 현재 회차를 바꾸기 전에 중단
        } // 무효 저장 처리 종료
        ConsumableItemData[] consumables = new ConsumableItemData[snapshot.consumables.Length]; // 기존 열 칸과 추가칸 복원
        for (int index = 0; index < snapshot.consumables.Length; index++) // 저장 소모품 슬롯 순회
        { // 소모품 검사 시작
            string id = snapshot.consumables[index]; // 저장 소모품 식별자 조회
            if (string.IsNullOrEmpty(id)) // 빈 슬롯 확인
            { // 빈 슬롯 처리 시작
                continue; // 다음 슬롯 확인
            } // 빈 슬롯 처리 종료
            ConsumableItemData item = FindUnique(catalog.Consumables, id, candidate => candidate.ItemId); // 소모품 원본 조회
            if (item == null || !item.IsValidData()) // 소모품 원본 유효성 확인
            { // 잘못된 소모품 처리 시작
                return false; // 복원 불가 반환
            } // 잘못된 소모품 처리 종료
            if (index == ConsumableInventoryRuntime.SlotCount && !(item is PotionData)) // 추가칸에 저장된 비포션 검사
            { // 무효 소모품 처리 시작
                return false; // 전체 저장 복원 중단
            } // 무효 소모품 처리 종료
            consumables[index] = item; // 저장 슬롯 소모품 보관
        } // 소모품 검사 종료
        run = new ResolvedRun // 검증된 복원 자료 생성
        { // 복원 자료 설정 시작
            snapshot = snapshot, // 탐사 저장 정보 설정
            party = party, // 파티 원본 목록 설정
            sourceCards = sourceCards, // 시작 덱 원본 목록 설정
            cards = cards, // 회차 덱 목록 설정
            relics = relics, // 유물 원본 목록 설정
            consumables = consumables, // 소모품 슬롯 설정
            shopCatalog = shopCatalog // 상점 원본 목록 설정
        }; // 복원 자료 설정 종료
        return true; // 전체 검증 성공 반환
    } // 저장 검증 종료
    private static bool TryResolveCard(RunCard saved, CharacterRosterData roster, SaveAssetCatalog catalog, HashSet<string> partyIds, out RunDeckCardEntry result) // 저장 카드 원본과 강화 검증
    { // 카드 검증 시작
        result = null; // 실패 기본값 설정
        if (saved == null || string.IsNullOrWhiteSpace(saved.owner) || !partyIds.Contains(saved.owner)) // 저장 카드 소유자 확인
        { // 카드 소유자 오류 시작
            return false; // 카드 복원 불가 반환
        } // 카드 소유자 오류 종료
        CardData card = FindUnique(catalog.Cards, saved.card, item => item.CardId); // 카드 원본 조회
        CharacterData owner = FindUnique(roster.Characters, saved.owner, item => item.CharacterId); // 카드 소유자 원본 조회
        if (card == null || owner == null || saved.upgrade < 0 || saved.upgrade > CardUpgradeProfileCatalog.GetMaximumUpgradeLevel(card)) // 카드와 강화 단계 범위 확인
        { // 카드 원본 오류 시작
            return false; // 카드 복원 불가 반환
        } // 카드 원본 오류 종료
        RunDeckCardEntry entry = new RunDeckCardEntry(card, owner); // 회차 카드 생성
        for (int level = 0; level < saved.upgrade; level++) // 저장 강화 단계 순회
        { // 카드 강화 복원 시작
            if (!entry.TryUpgrade()) // 카드 강화 적용 확인
            { // 카드 강화 실패 처리 시작
                return false; // 카드 복원 불가 반환
            } // 카드 강화 실패 처리 종료
        } // 카드 강화 복원 종료
        result = entry; // 검증된 카드 반환값 설정
        return true; // 카드 검증 성공 반환
    } // 카드 검증 종료
    private static T FindUnique<T>(IReadOnlyList<T> items, string id, Func<T, string> getId) where T : UnityEngine.Object // 식별자로 유일한 원본 조회
    { // 원본 조회 시작
        if (items == null || string.IsNullOrWhiteSpace(id)) // 목록과 식별자 확인
        { // 조회 입력 오류 시작
            return null; // 일치 원본 없음 반환
        } // 조회 입력 오류 종료
        T found = null; // 일치 원본 보관 변수
        foreach (T item in items) // 원본 목록 순회
        { // 원본 비교 시작
            if (item == null || getId(item) != id) // 원본과 식별자 일치 확인
            { // 불일치 처리 시작
                continue; // 다음 원본 확인
            } // 불일치 처리 종료
            if (found != null && found != item) // 중복 식별자 확인
            { // 중복 처리 시작
                return null; // 모호한 원본 차단
            } // 중복 처리 종료
            found = item; // 일치 원본 보관
        } // 원본 비교 종료
        return found; // 유일한 일치 원본 반환
    } // 원본 조회 종료
    private static bool ValidEscapeGoldClaims(RunSnapshot snapshot) // 도주 골드 지급 목록 검증
    { // 지급 기록 검사 시작
        var seen = new HashSet<string>(StringComparer.Ordinal); // 중복 조우 식별자 검사
        long claimedGold = 0; // 지급 기록 전체 골드 합계 준비
        foreach (var claim in snapshot.escapeGoldClaims) // 저장된 도주 기록 검사
        { // 개별 지급 기록 검사 시작
            if (claim == null || string.IsNullOrWhiteSpace(claim.encounter) || claim.gold <= 0 || !seen.Add(claim.encounter) || Array.IndexOf(snapshot.cleared, claim.encounter) >= 0) // 누락과 중복 및 승리 정산 잔존 확인
            { // 잘못된 지급 기록 처리 시작
                return false; // 자료 복원 차단
            } // 잘못된 지급 기록 처리 종료
            claimedGold += claim.gold; // 이미 지급한 골드 합산
        } // 개별 지급 기록 검사 종료
        return claimedGold <= snapshot.runGold; // 실제 탐사 획득량 이내 확인
    } // 지급 기록 검사 종료
    private static bool ValidSpecialStock(ShopRunManager.RelicReserve[] reserves, ShopCatalogData catalog) // 원본을 조회해 저장된 특수 재고의 복원 가능 여부 확인
    { // 특수 상품 저장 검증 시작
        if (reserves == null) // 방별 특수 상품이 없던 기존 저장 확인
        { // 구버전 저장 처리 시작
            return true; // 기본 진열만 복원 허용
        } // 구버전 저장 처리 종료
        var sheet = CardSheetCatalog.Load(); // 현재 등록 카드의 시트 희귀도 색인
        foreach (var stock in reserves) // 저장한 방 단위 진열과 판매 정보 순회
        { // 방별 재고 확인 시작
            if (stock == null || !stock.specialCardSold && string.IsNullOrEmpty(stock.specialCardOfferId)) // 일반 재고뿐인 방 확인
            { // 일반 재고 처리 시작
                continue; // 기존 방의 특수 상품 대조 제외
            } // 일반 재고 처리 종료
            bool found = false; // 현재 저장의 특수 상품 원본 발견 여부
            foreach (var offer in catalog.Offers) // 실물 보상으로 구현된 상점 카탈로그 원본 확인
            { // 실제 특수 카드 원본 대조 시작
                if (offer.OfferId == stock.specialCardOfferId && ShopRunManager.IsSpecialCandidate(offer, sheet)) // 기존 원본의 유일한 판매 가능 특수 카드 확인
                { // 선택한 특수 카드 존재 처리 시작
                    found = true; // 실제 구매 및 소유자 선택 가능 상태 확인
                    break; // 지정 상품의 추가 검색 종료
                } // 선택한 특수 카드 존재 처리 종료
            } // 실제 특수 카드 원본 대조 종료
            if (!found || string.IsNullOrWhiteSpace(stock.roomId)) // 판매된 특수 재고의 실제 상점 방과 원본 확인
            { // 누락된 재고 복원 방지 시작
                return false; // 유효하지 않은 특수 재고 상태 거부
            } // 누락된 재고 복원 방지 종료
        } // 방별 재고 확인 종료
        return true; // 기존 및 특수 상품의 정상 저장 허용
    } // 특수 상품 저장 검증 종료

    private static bool ValidIds(string[] ids) // 저장 식별자 목록 검증
    { // 식별자 검사 시작
        if (ids == null) // 목록 누락 확인
        { // 목록 누락 처리 시작
            return false; // 유효하지 않은 목록 반환
        } // 목록 누락 처리 종료
        HashSet<string> seen = new HashSet<string>(); // 중복 검사 집합 생성
        foreach (string id in ids) // 저장 식별자 순회
        { // 개별 식별자 검사 시작
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) // 빈 식별자와 중복 확인
            { // 잘못된 식별자 처리 시작
                return false; // 유효하지 않은 목록 반환
            } // 잘못된 식별자 처리 종료
        } // 개별 식별자 검사 종료
        return true; // 정상 식별자 목록 반환
    } // 식별자 검사 종료
    private static bool IsFinite(float value) // 좌표와 노출도 유한성 검사
    { // 유한성 검사 시작
        return !float.IsNaN(value) && !float.IsInfinity(value); // 유효한 숫자 여부 반환
    } // 유한성 검사 종료
} // 탐사 저장 서비스 종료
