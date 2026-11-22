using System; // 실제 검사 경로와 직렬화 자료 사용
using System.Collections; // Unity 플레이 환경 전환
using System.IO; // 독립 저장 파일 읽기와 기록
using System.Linq; // 실제 보유 순서와 원본 카드 비교
using NUnit.Framework; // 이전 저장과 원자적 거부 검사
using UnityEngine; // 실제 JSON 직렬화와 원본 리소스
using UnityEngine.TestTools; // 실제 게임 저장 관리자가 동작하는 모드

public sealed class Day290LegacySaveCompatibilityTests // 이전 두 저장 양식의 실제 복원과 잘못된 값 차단 회귀
{ // 이전 저장 검사 시작
    private const string FirstOwner = "ARC_00"; // 110일차부터 유지된 광대의 실제 캐릭터 식별자
    private const string SecondOwner = "ARC_01"; // 110일차부터 유지된 마술사의 실제 캐릭터 식별자
    private const string Attack = "CRD_TEMP_ATTACK"; // 110일차 실제 시작 공격 카드 원본
    private const string Defense = "CRD_TEMP_DEFENSE"; // 110일차 실제 시작 방어 카드 원본
    private const string Radio = "RLC_VINTAGE_RADIO"; // 246일차 저장 카탈로그의 실제 구형 무전기
    private const string Lantern = "RLC_AFTERGLOW_LANTERN"; // 246일차 저장 카탈로그의 실제 잔광 등불

    [UnityTest] // 최초 구역 없는 탐사 저장의 실제 복원과 초기화
    public IEnumerator Day110RunRestoresOldCardsOwnersAndLegacyPoolAsWinterfall() // 최초 실제 저장 양식과 월드맵 기본값 확인
    { // 최초 탐사 저장 복원 검사 시작
        RequireVerificationProfile(); // 현재 사용자 저장과 분리된 검증 환경 확인
        yield return new EnterPlayMode(); // 원래 저장 서비스가 동작하는 플레이 모드 진입
        string path = TemporaryPath(); // 110일차 형식을 쓸 독립 검증 경로
        try // 검증 파일의 종료 처리 보장
        { // 실제 이전 저장 구성 시작
            var session = PrepareModernArea(); // 다른 구역의 최신 출전 상태를 미리 준비
            Assert.IsTrue(PartyPresetStore.Select(2)); // 이전 거점 파일 복원 전 현재 편성 번호 변경
            var old = CreateDay110Run(); // 71100e4 Commit의 실제 기존 탐사 필드에서만 직렬화할 자료
            Assert.IsTrue(LobbySaveService.CanLoadJson(old.lobbyJson)); // 이전 거점 자료의 실제 검증 허용
            string json = JsonUtility.ToJson(old); // 당시 실제 지원 필드만 포함한 회차 직렬화
            StringAssert.DoesNotContain("\"zoneVersion\"", json); // 월드맵 이전에 존재하지 않은 출전 필드 부재 확인
            StringAssert.DoesNotContain("\"encounterPoolVersion\"", json); // 첫 저장이 조우 버전 필드를 기록하지 않았던 사실 확인
            File.WriteAllText(path, json); // 검증용 이전 양식의 실제 파일 기록
            Assert.IsTrue(ExplorationSaveService.CanLoad(path)); // 기존 형식의 전체 원본 검증
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 기존 합법 저장의 실제 현재 서비스 복원
            Assert.AreEqual(ExpeditionZoneId.WinterfallAshPlains, session.CurrentZone); // 최초 저장에 없는 구역의 실제 기본값
            Assert.AreEqual(1, session.CurrentZoneLevel); // 지역 레벨의 실제 기본값 복원
            Assert.IsFalse(session.IsZoneBossReservedForRun); // 이전 파일의 예약 없는 기본값 복원
            Assert.AreEqual(0, session.EncounterPoolVersion); // 첫 저장의 이전 조우 풀 버전 유지
            Assert.AreEqual(0, session.EventPoolVersion); // 이전 저장의 이전 이벤트 풀 유지
            Assert.AreEqual(old.seed, session.CurrentFloorSeed); // 이전 실제 층의 생성 시드 유지
            Assert.AreEqual(old.position, session.GetPlayerSpawnPosition(Vector3.zero)); // 이전 실제 복귀 위치 유지
            CollectionAssert.AreEqual(new[] { FirstOwner, SecondOwner }, PartyLoadoutRuntime.Active.Party.Members.Select(unit => unit.CharacterId)); // 실제 저장 파티 순서 유지
            CollectionAssert.AreEqual(new[] { (Attack, FirstOwner, 1), (Defense, SecondOwner, 0) }, CurrentCards()); // 당시 원본 카드의 소유자와 실제 강화 유지
            Assert.AreEqual(0, PartyPresetStore.Selected); // 편성 필드 이전 파일의 원래 첫 편성 기본값
            Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.FracturedEarth)); // 다른 슬롯의 최신 진행도를 가져오지 않는 이전 거점 복원
            var oldPool = ExpeditionZoneEncounters.LoadRandom(session.CurrentZone, session.EncounterPoolVersion); // 이전 저장에서 실제 선택되는 윈터폴 풀 조회
            Assert.IsTrue(oldPool.Any(encounter => encounter.EncounterId == "ENC_TEST_C")); // 110일차의 실제 이전 보스 데이터 유지
            Assert.IsFalse(oldPool.Any(encounter => encounter.EncounterId == "ENC_WF_ASHSTORM_CORE")); // 당시 조우 풀 이후 보스의 유입 방지
            Assert.AreEqual(37, PlayerResourceManager.Instance.Gold); // 최초 저장에 보관한 전체 골드 복원
            Assert.AreEqual(0, PlayerResourceManager.Instance.ExplorationGold); // 임시 획득 구분이 없던 이전 기록의 원래 잔액 보존
        } // 실제 이전 저장 구성 종료
        finally // 이전 형식 검사 파일 정리
        { // 사용자 경로와 별개의 검사 파일만 정리
            ExplorationSaveService.Remove(path); // 방금 생성한 실제 저장과 정상 이전 사본 제거
        } // 사용자 경로와 별개의 검사 파일만 정리 종료
        yield return new ExitPlayMode(); // 검사 저장 관리자와 실행 환경 정리
    } // 최초 탐사 저장 복원 검사 종료

    [UnityTest] // 월드맵 이전 유물과 재화 추적 저장의 실제 재저장 호환
    public IEnumerator Day246RunKeepsRelicOrderUpgradeAndLedgerWhenSavedAgain() // 이전 회차 데이터와 새 지역 기본값의 동시 보존
    { // 월드맵 이전 저장의 두 차례 복원 검사 시작
        RequireVerificationProfile(); // 현재 사용자의 실제 탐사 파일 보호
        yield return new EnterPlayMode(); // 현재 게임 서비스와 원본 리소스 준비
        string legacyPath = TemporaryPath(); // 이전 실제 필드 양식 파일
        string migratedPath = TemporaryPath(); // 처음 실제 복원 뒤의 현재 형식 저장 파일
        try // 두 실제 검사 파일 정리 보장
        { // 이전 양식과 현재 양식의 실제 서비스 실행
            var session = PrepareModernArea(); // 이전 데이터가 초기화해야 할 독립 최신 구역 선택
            var old = CreateDay246Run(); // 월드맵 전의 실제 유물과 카드 및 추적 골드 입력
            string legacyJson = JsonUtility.ToJson(old); // Git 자료에서 확인한 246일차 필드만으로 직렬화
            StringAssert.DoesNotContain("\"zoneVersion\"", legacyJson); // 현재 구역 형식을 포함하지 않는 과거 저장 검증
            File.WriteAllText(legacyPath, legacyJson); // 검증 전용 경로에 작성한 실제 이전 형태
            Assert.IsTrue(ExplorationSaveService.Load(legacyPath)); // 이전 유물과 시드를 기존 게임 경로로 실제 복원
            VerifyDay246State(session, old); // 선택과 유물 및 원래 재화 추적 상태 확인
            Assert.IsTrue(ExplorationSaveService.Save(migratedPath, old.position)); // 이전 기록을 원래 게임의 현재 형식으로 다시 저장
            var rewritten = JsonUtility.FromJson<ExplorationSaveService.RunSnapshot>(File.ReadAllText(migratedPath)); // 실제 저장 서비스가 작성한 새 기록 읽기
            Assert.AreEqual(1, rewritten.version); // 이전과 현재의 공식 최상위 지원 버전 확인
            Assert.AreEqual(1, rewritten.zoneVersion); // 실제 새 구역 필드가 생기는 재저장 확인
            CollectionAssert.AreEqual(old.relics, rewritten.relics); // 이전 저장의 실제 획득 순서와 ID 보존
            Assert.IsTrue(ExplorationSaveService.Load(migratedPath)); // 새로 기록한 실제 파일을 다시 복원
            VerifyDay246State(session, old); // 재저장 전과 같은 이전 데이터 복원 검사
        } // 이전 양식과 현재 양식의 실제 서비스 실행 종료
        finally // 독립 검사 파일 두 개 정리
        { // 현재 사용자 파일과 분리된 저장 정리
            ExplorationSaveService.Remove(legacyPath); // 검증용 이전 원본 제거
            ExplorationSaveService.Remove(migratedPath); // 검증용 재저장 원본 제거
        } // 현재 사용자 파일과 분리된 저장 정리 종료
        yield return new ExitPlayMode(); // 네이티브 저장 테스트 실행 환경 정리
    } // 월드맵 이전 저장의 두 차례 복원 검사 종료

    [UnityTest] // 실제 잘못된 카드와 유물 ID의 저장 원자성 검사
    public IEnumerator InvalidLegacyEntriesLeaveAnActiveModernRunUntouched() // 이전 양식의 세 잘못된 값과 현재 상태의 완전 유지 비교
    { // 원자적 저장 거부 검사 시작
        RequireVerificationProfile(); // 원래 사용자의 현재 탐사와 저장에 영향 없는 환경 선택
        yield return new EnterPlayMode(); // 실제 전투·탐사 원본으로 정상 현재 상태 준비
        string path = TemporaryPath(); // 독립 파일의 원본 및 복구본 없는 검증 경로
        try // 손상 검사 파일 제거 보장
        { // 실제 로드가 거절해야 할 회차 수치 검증 시작
            var valid = CreateDay246Run(); // 실제로 복원되는 기준의 원형 저장
            File.WriteAllText(path, JsonUtility.ToJson(valid)); // 손상 대비 기준이 되는 이전 파일 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 검증비교가 유효한 이전 입력에서 시작
            var session = PrepareModernArea(); // 유지되어야 하는 별개 실제 구역 확정
            var active = PartyLoadoutRuntime.Active; // 현재 유효 편성의 실제 원본 참조
            var cards = CurrentCards(); // 현재 유효 덱의 실제 값 보관
            var relicIds = CurrentRelicIds(); // 현재 유효 유물의 실제 순서와 식별자 보관
            Assert.IsTrue(LobbySaveService.TryCapture(out string lobby)); // 현재 거점 자원과 지역 진행 전체 스냅샷
            int seed = session.CurrentFloorSeed; // 계속 보존해야 하는 최신 회차 지도 시드
            var corrupt = new Action<Day246Run>[] // 실제 식별자 및 강화 검증의 세 실패 분기
            { // 정상 이전 저장에 한 번에 한 필드만 손상 시작
                snapshot => snapshot.cards[0].card = "CRD_DAY290_UNKNOWN", // 당시나 현재 유효하지 않은 카드 원본
                snapshot => snapshot.cards[0].upgrade = int.MaxValue, // 실제 카드 강화의 최대 범위 초과
                snapshot => snapshot.relics[1] = snapshot.relics[0] // 같은 원본이 중복 저장된 유물 획득 목록
            }; // 정상 이전 저장에 한 번에 한 필드만 손상 종료
            foreach (var damage in corrupt) // 독립적으로 하나의 다른 손상 검사
            { // 원래 파일 전체 검증과 현재 상태의 불변 검사 시작
                var invalid = CreateDay246Run(); // 손상된 이전 시나리오의 독립 기준
                damage(invalid); // 각 검사에서 의도한 잘못된 저장 필드 한 건
                File.WriteAllText(path, JsonUtility.ToJson(invalid)); // 이전 파일 형식에 실제 잘못된 값 기록
                Assert.IsFalse(ExplorationSaveService.CanLoad(path)); // 기존 네이티브 검증의 선행 거부 확인
                Assert.IsFalse(ExplorationSaveService.Load(path)); // 유효하지 않은 파일이 실제 복원되는 경로의 선행 차단
                Assert.AreSame(active, PartyLoadoutRuntime.Active); // 부분 복원으로 현재 실제 편성이 교체되지 않음 확인
                CollectionAssert.AreEqual(cards, CurrentCards()); // 기존 현재 카드와 소유자 및 강화 전체 보존
                CollectionAssert.AreEqual(relicIds, CurrentRelicIds()); // 기존 실제 유물 원본 및 획득 순서 보존
                Assert.AreEqual(ExpeditionZoneId.FracturedEarth, session.CurrentZone); // 현재 실제 구역을 이전 파일 기본값으로 덮지 않음 확인
                Assert.AreEqual(seed, session.CurrentFloorSeed); // 기존 회차 시드 보존
                Assert.IsTrue(LobbySaveService.TryCapture(out string current)); // 잘못된 탐사 복원 이후 실제 거점 상태 재수집
                Assert.AreEqual(lobby, current); // 이전 거점 JSON의 부분 적용과 원래 자원 덮어쓰기 없음 확인
            } // 원래 파일 전체 검증과 현재 상태의 불변 검사 종료
        } // 실제 로드가 거절해야 할 회차 수치 검증 종료
        finally // 유일한 검사 파일 제거
        { // 다른 저장 슬롯에 영향을 주지 않는 독립 경로 정리
            ExplorationSaveService.Remove(path); // 방금 사용한 실제 파일과 관련 사본 제거
        } // 다른 저장 슬롯에 영향을 주지 않는 독립 경로 정리 종료
        yield return new ExitPlayMode(); // 플레이 설정과 독립 상태 환경 정리
    } // 원자적 저장 거부 검사 종료

    private static ExplorationSessionManager PrepareModernArea() // 이전 파일로 변해야 하는 실제 신규 지역 선택 구성
    { // 현재 지역 상태 준비 시작
        ExpeditionZoneProgress.Reset(); // 실제 계정과 분리된 새 원정 진행 시작
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.WinterfallAshPlains, 1, false); // 정식 규칙에서 원래 윈터폴 보스 클리어
        var session = ExplorationSessionManager.EnsureInstance(); // 정상 회차 관리자의 활성 지역 연결
        session.ResetExploration(); // 기존 테스트 회차의 실제 구역 이전 상태 제거
        Assert.IsTrue(session.BeginZoneExpedition(ExpeditionZoneId.FracturedEarth, 1)); // 해금된 새 구역의 실제 첫 단계 선택
        session.SetCurrentFloorSeed(290901); // 오래된 값과 다르게 보존 또는 복원할 확인용 시드
        return session; // 다음 실제 저장 처리에서 사용할 현재 세션 반환
    } // 현재 지역 상태 준비 종료

    private static Day110Run CreateDay110Run() // 71100e4에서 실제 작성했던 초대 저장 필드만 포함
    { // 처음 지원한 탐사 파일의 실제 회차 구성 시작
        return new Day110Run // 당시 실제 직렬화 버전과 원본 필드 입력
        { // 오래된 회차 저장 자료 구성 시작
            version = 1, // 110일차 실제 탐사 파일 버전
            floor = 1, // 원래 첫 실행 맵의 층 번호
            seed = 110921, // 당시 양식으로 유효한 독립 층 생성 시드
            position = new Vector3(1.5f, 2.5f, 0f), // 실제 탐사에서 복귀할 수 있는 저장 위치
            party = new[] { FirstOwner, SecondOwner }, // 당시부터 실재한 광대와 마술사의 순서
            sourceCards = new[] { new ExplorationSaveService.RunCard { card = Attack, owner = FirstOwner, upgrade = 0 }, new ExplorationSaveService.RunCard { card = Defense, owner = SecondOwner, upgrade = 0 } }, // 원래 획득 전 시작 카드와 각 실제 소유자
            cards = new[] { new ExplorationSaveService.RunCard { card = Attack, owner = FirstOwner, upgrade = 1 }, new ExplorationSaveService.RunCard { card = Defense, owner = SecondOwner, upgrade = 0 } }, // 당시에도 저장한 공격 한 강과 실제 소유자
            cleared = Array.Empty<string>(), // 첫 검증 층의 원래 미완료 조우 목록
            events = Array.Empty<string>(), // 첫 검증 층의 원래 미처리 사건 목록
            relics = Array.Empty<string>(), // 당시 아직 확장하지 않았던 유물 목록
            consumables = new string[10], // 110일차 열 칸의 실제 기본 소모품 저장
            shopRoom = string.Empty, // 상점을 방문하기 전의 빈 방 식별자
            purchasedOffers = Array.Empty<string>(), // 당시에 실제 저장한 비어 있는 구매 목록
            lobbyJson = JsonUtility.ToJson(CreateDay110Lobby()) // 당시 실제 거점 저장 필드에 맞춘 중첩 JSON
        }; // 오래된 회차 저장 자료 구성 종료
    } // 처음 지원한 탐사 파일의 실제 회차 구성 종료

    private static Day110Lobby CreateDay110Lobby() // 71100e4 거점의 당시 실제 필드와 상태
    { // 초기 거점 직렬화 자료 구성 시작
        return new Day110Lobby // 월드맵 및 편성 프리셋 이전의 저장
        { // 초기 거점 저장값 대입 시작
            version = 1, // 당시 실제 거점 저장 버전
            gold = 37, // 당시 보관하던 실제 전체 골드 수치
            screw = 3, // 실제 나사 계정 보유값
            ironPlate = 5, // 실제 철판 계정 보유값
            wire = 2, // 실제 전선 계정 보유값
            level = 2, // 과거에서도 유효한 성장 레벨
            experience = 3, // 해당 성장 수준에서 가능한 실제 경험치
            affinity = 4, // 거점 저장에 포함한 호감도
            facilities = Enumerable.Range(0, 10).Select(index => new Facility { type = index, level = 0 }).ToArray(), // 당시와 현재에 같은 번호를 가진 정식 열 설비
            characters = new[] { new SavedCharacter { id = FirstOwner, health = 18, mental = 31, deaths = 0, recovery = 0 }, new SavedCharacter { id = SecondOwner, health = 20, mental = 32, deaths = 0, recovery = 0 } } // 초기 실제 파티원의 지속 체력과 정신력
        }; // 초기 거점 저장값 대입 종료
    } // 초기 거점 직렬화 자료 구성 종료

    private static Day246Run CreateDay246Run() // 70dee03이 실제 지원한 탐사 진행과 현재 원본 보관
    { // 이전 유물·진행·재화 추적 자료 준비 시작
        var snapshot = JsonUtility.FromJson<Day246Run>(JsonUtility.ToJson(CreateDay110Run())); // 역사적으로 유지한 원래 공통 저장값 조회
        snapshot.encounterPoolVersion = 2; // 246일차 실제 저장의 윈터폴 조우 목록
        snapshot.eventPoolVersion = 8; // 246일차 실제 층에서 지원한 여덟 번째 사건 목록
        snapshot.seed = 246921; // 당시에도 지원한 별개의 실제 층 생성 시드
        snapshot.wandererCry = new WandererCryRunState(); // 당시 저장기에 항상 들어가는 기본 방랑자 기록
        snapshot.compass = new CrackedCompassRunState(); // 당시 실제 탐사에서 보관한 미사용 나침반 상태
        snapshot.well = new CharredWellRunState(); // 당시 방문하지 않은 실제 우물 상태
        snapshot.cart = new BurntCartRunState(); // 당시 미사용 수레와 매복 기록
        snapshot.avoidedEncounters = Array.Empty<string>(); // 당시 저장기에 포함되는 회피 없는 현재 목록
        snapshot.escapeGoldClaims = Array.Empty<ExplorationSaveService.EscapeGoldClaim>(); // 같은 조우의 도주 정산이 없던 원래 목록
        snapshot.shopRelicReserves = Array.Empty<ShopRunManager.RelicReserve>(); // 방문 전 상점의 실제 빈 예약 목록
        snapshot.relics = new[] { Radio, Lantern }; // 246일차 실제 등록 파일 두 유물의 현재 획득 순서
        snapshot.runGold = 5; // 실제 탐사에서 벌어서 남은 골드의 전체 획득값
        snapshot.hasResourceLedger = true; // 당시 실제 지원한 이전 회차의 임시 재화 구분
        snapshot.temporaryGold = 5; // 별개 골드 누적에서 실제 탐사에만 남는 양
        var lobby = JsonUtility.FromJson<Day246Lobby>(snapshot.lobbyJson); // 실제 공통 거점 자료를 246일차 프리셋 있는 양식으로 구성
        lobby.presetVersion = 1; // 246일차에 저장되는 선택 편성 형식
        lobby.presets = new PartyPresetSnapshot // 당시 저장하고 현재에도 지원한 기존 프리셋 값
        { // 당시 거점 편성 자료 시작
            selected = 1, // 실제 두 번째 편성 선택 번호
            entries = Enumerable.Range(0, 4).Select(index => new PartyPresetEntry // 246일차부터 있는 네 편성의 원래 입력값
            { // 이전 실제 프리셋의 캐릭터 및 위치 기록 시작
                members = index == 1 ? new[] { FirstOwner, SecondOwner } : Array.Empty<string>(), // 이전 실제 출전 순서의 보존
                slots = index == 1 ? new[] { string.Empty, string.Empty, SecondOwner, FirstOwner } : new string[4] // 구역 이전에도 보관한 사전 슬롯 위치
            }).ToArray() // 실제 네 편성의 캐릭터와 자리 데이터 생성
        }; // 당시 거점 편성 자료 종료
        snapshot.lobbyJson = JsonUtility.ToJson(lobby); // 구역 상태를 포함하지 않는 당시 거점 파일
        return snapshot; // 현재 추가 정보가 없는 실제 당시 탐사 값
    } // 이전 유물·진행·재화 추적 자료 준비 종료

    private static void VerifyDay246State(ExplorationSessionManager session, Day246Run expected) // 실제 이전 저장 데이터 불변 확인
    { // 이전 회차의 핵심 현재 복원값 검사 시작
        Assert.AreEqual(ExpeditionZoneId.WinterfallAshPlains, session.CurrentZone); // 월드맵 이전 선택은 윈터폴 기본값
        Assert.AreEqual(1, session.CurrentZoneLevel); // 구역 난이도가 없던 실제 이전 파일 기본값
        Assert.AreEqual(expected.encounterPoolVersion, session.EncounterPoolVersion); // 당시 원래 적 조우 목록 버전 유지
        Assert.AreEqual(expected.eventPoolVersion, session.EventPoolVersion); // 당시 원래 사건 목록 버전 유지
        Assert.AreEqual(expected.seed, session.CurrentFloorSeed); // 이전 시드와 해당 실제 층 배치의 유지
        CollectionAssert.AreEqual(new[] { (Attack, FirstOwner, 1), (Defense, SecondOwner, 0) }, CurrentCards()); // 원래 카드 순서와 실제 소유자 및 강화를 그대로 복원
        CollectionAssert.AreEqual(expected.relics, CurrentRelicIds()); // 실제 이전 유물의 원본 식별자와 획득 순서 확인
        Assert.AreEqual(5, PlayerResourceManager.Instance.ExplorationGold); // 전체 보유값과 독립인 이번 탐사 골드 확인
        Assert.AreEqual(37, PlayerResourceManager.Instance.Gold); // 중복 지급 없이 당시 거점 골드 전체 보존
        Assert.AreEqual(1, PartyPresetStore.Selected); // 실제 이전 거점의 두 번째 선택 편성 복원
        CollectionAssert.AreEqual(new[] { string.Empty, string.Empty, SecondOwner, FirstOwner }, PartyPresetStore.GetSlots(1)); // 이전 프리셋의 빈 칸과 실제 출전 자리 유지
    } // 이전 회차의 핵심 현재 복원값 검사 종료

    private static (string, string, int)[] CurrentCards() => RunDeckManager.Instance.Cards.Select(card => (card.Card.CardId, card.Owner.CharacterId, card.UpgradeLevel)).ToArray(); // 실제 카드 식별자와 소유자 및 강화 스냅샷
    private static string[] CurrentRelicIds() => RelicRunManager.Instance.Inventory.OwnedRelics.Select(relic => relic.RelicId).ToArray(); // 실제 저장과 같은 유물 획득 순서 스냅샷
    private static string TemporaryPath() => Path.Combine(Application.temporaryCachePath, "day290-legacy-" + Guid.NewGuid().ToString("N") + ".json"); // 현재 사용자의 슬롯과 관련 없는 독립 파일

    private static void RequireVerificationProfile() // 원래 계정의 진행과 분리된 검증 설정 확인
    { // 분리한 테스트 환경인지 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 현재 파일 저장과 Unity 초기화의 독립 환경 조건
        { // 기본 원래 플레이 환경 제외 시작
            Assert.Ignore("저장 경로를 분리한 검증 프로젝트에서 실행"); // 뒤에 실제 사용하는 자원과 사용자 기존 파일 보호
        } // 기본 원래 플레이 환경 제외 종료
    } // 분리한 테스트 환경인지 검사 종료

    [Serializable] // 110일차의 실제 Unity 탐사 DTO 복원 형식
    private class Day110Run // Git 71100e4의 원래 탐사 RunSnapshot 필드
    { // 실제 최초 탐사 저장 필드 시작
        public int version; // 공식 초기 탐사 형식 번호
        public int floor; // 당시 현재 층
        public int seed; // 당시 맵 시드
        public Vector3 position; // 저장한 플레이어의 원래 위치
        public string[] party; // 당시 편성한 원본 캐릭터의 실제 순서
        public ExplorationSaveService.RunCard[] sourceCards; // 기존 시작 카드식별자와 소유자 및 강화 구조
        public ExplorationSaveService.RunCard[] cards; // 당시 실제 탐사 덱 식별자 목록
        public string[] cleared; // 실제 정복한 조우 원본 목록
        public string[] events; // 그때 처리한 사건과 특수방
        public string[] relics; // 기록을 지원한 당시 원래 유물 목록
        public string[] consumables; // 초기 원본 소모품 열 칸
        public string shopRoom; // 실제 기존 확인한 상점 방
        public string[] purchasedOffers; // 당시 실제로 구입 완료한 목록
        public bool restUsed = false; // 초기 휴식 사용 여부
        public int hazardHealth = 0; // 당시 대기 위험 체력 피해
        public int hazardMental = 0; // 당시 대기 위험 정신력 피해
        public float hazardExposure = 0f; // 당시 실제 위험 노출도
        public int runExperience = 0; // 당시 탐사 경험치 합계
        public int runGold = 0; // 당시 탐사 골드 합계
        public int runScrew = 0; // 당시 탐사 나사 합계
        public int runIronPlate = 0; // 당시 탐사 철판 합계
        public int runWire = 0; // 당시 탐사 전선 합계
        public string lobbyJson; // 당시 탐사에 동봉한 거점 전체 자료
    } // 실제 최초 탐사 저장 필드 종료

    [Serializable] // 월드맵 전 246일차의 실제 추가 탐사 필드
    private sealed class Day246Run : Day110Run // Git 70dee03의 월드맵 이전 탐사 RunSnapshot 필드
    { // 246일차에 포함한 기존 증가 필드 시작
        public int encounterPoolVersion; // 당시 구역과 별개인 기존 조우 목록 버전
        public int eventPoolVersion; // 해당 층의 실제 사건 목록 버전
        public bool hasOptionalBossRoom = false; // 선택 보스 방이 없는 당시 합법 기록
        public Vector2Int optionalBossRoom = default; // 선택 방이 없는 회차의 기본 좌표
        public string pendingMapFragment = string.Empty; // 대기 지도 조각이 없는 실제 회차 기록
        public bool usedSealedMap = false; // 선택 지도의 사용을 기록하던 이전 형식
        public GlassFieldEffect glassFieldEffects = GlassFieldEffect.None; // 다음 전투에 대기하는 들판 효과
        public WandererCryRunState wandererCry = new WandererCryRunState(); // 별도 방랑자 선택을 사용하지 않은 원래 상태
        public CrackedCompassRunState compass = new CrackedCompassRunState(); // 이전 나침반 발견과 사용의 실제 상태
        public string[] avoidedEncounters = Array.Empty<string>(); // 지도 조각으로 회피했던 이전 조우
        public ExplorationSaveService.EscapeGoldClaim[] escapeGoldClaims = Array.Empty<ExplorationSaveService.EscapeGoldClaim>(); // 도주 보상 선지급이 없는 행 목록
        public CharredWellRunState well = new CharredWellRunState(); // 우물 회복과 층 선택의 이전 상태
        public BurntCartRunState cart = new BurntCartRunState(); // 수레 매복과 할인 원본 상태
        public ShopRunManager.RelicReserve[] shopRelicReserves = Array.Empty<ShopRunManager.RelicReserve>(); // 방문한 방의 이전 특수 상점 재고
        public bool shopDrawerDiscountUsed = false; // 당시 첫 상점의 사용한 할인 여부
        public bool hasResourceLedger; // 획득 재화와 이전 보유값을 구분하는 저장 여부
        public int pendingSpecimenDraw = 0; // 다음 카드 드로우가 예약되지 않은 상태
        public int pendingSpecimenActionPoints = 0; // 다음 행동력이 예약되지 않은 상태
        public int temporaryGold; // 실제 탐사에서 보관 중인 임시 골드
        public int temporaryScrew = 0; // 당시 다음 전멸에 잃을 이번 탐사 나사
        public int temporaryIronPlate = 0; // 당시 다음 전멸에 잃을 이번 탐사 철판
        public int temporaryWire = 0; // 당시 다음 전멸에 잃을 이번 탐사 전선
    } // 246일차에 포함한 기존 증가 필드 종료

    [Serializable] // 처음 등장한 당시 버전 일 거점의 실제 형태
    private class Day110Lobby // Git 71100e4의 원래 거점 SaveData 필드
    { // 실제 최초 거점 저장 필드 시작
        public int version; // 당시에 작성한 거점 버전
        public int gold; // 실제 거점 골드 합계
        public int screw; // 거점의 나사 합계
        public int ironPlate; // 거점의 철판 합계
        public int wire; // 거점의 전선 합계
        public int level; // 실제 현재 성장 레벨
        public int experience; // 당시 해당 레벨의 경험치
        public int affinity; // 실제 기존 호감도
        public Facility[] facilities; // 오리지널 당시 열 설비
        public SavedCharacter[] characters; // 처음과 지금의 실제 캐릭터 기록
    } // 실제 최초 거점 저장 필드 종료

    [Serializable] // 월드맵 이전 246일차 거점의 추가 편성 필드
    private sealed class Day246Lobby : Day110Lobby // 당시 기존 거점 값과 프리셋을 함께 직렬화
    { // 246일차에 존재한 추가 거점 필드 시작
        public int presetVersion; // 당시 편성 저장의 실제 버전
        public PartyPresetSnapshot presets; // 기존 네 편성 원본 상태
    } // 246일차에 존재한 추가 거점 필드 종료

    [Serializable] // 당시 설비 번호와 레벨의 실제 저장 형식
    private sealed class Facility // 기존 거점의 한 설비
    { // 실제 과거 설비 자료 시작
        public int type; // 현재와 같은 기존 설비 번호
        public int level; // 당시 선택한 실제 설비 등급
    } // 실제 과거 설비 자료 종료

    [Serializable] // 초기부터 복원하는 당시 캐릭터 정보
    private sealed class SavedCharacter // 실제 출전 캐릭터의 보관 값
    { // 최초 게임의 실제 저장 캐릭터 시작
        public string id; // 초기부터 사용한 원본 캐릭터 고유 번호
        public int health; // 당시 저장 체력
        public int mental; // 당시 저장 정신력
        public int deaths; // 당시 실제 사망 횟수
        public int recovery; // 당시 남은 회복에 필요한 탐사 횟수
    } // 최초 게임의 실제 저장 캐릭터 종료
} // 이전 저장 검사 종료
