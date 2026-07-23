# 7일차: 전투 덱 런타임 구조 구현

## 개발 목표

원본 카드 데이터와 전투 중 사용하는 카드 인스턴스를 분리하고, 전투 시작 시 원본 덱을 기반으로 런타임 덱을 생성하는 구조를 구현한다.

## 구현 내용

### 1. 카드 원본 데이터 구성

- `CardRank.cs` 생성
- `CardType.cs` 생성
- `CardData.cs` 생성
- ScriptableObject 기반 카드 원본 데이터 구조 구성
- 카드 ID, 이름, 설명, 비용, 등급, 종류, 강화 정보 관리

### 2. 전투용 카드 인스턴스 구성

- `CardInstance.cs` 생성
- 원본 카드와 전투 중 변경되는 데이터를 분리
- 카드별 고유 Instance ID 생성
- 전투 중 임시 비용 변경 기능 구성
- 원본 카드 비용이 변경되지 않도록 처리

### 3. 전투 덱 런타임 관리자 구현

- `BattleDeckRuntime.cs` 생성
- 전투 시작 시 원본 카드 목록 기반 런타임 카드 생성
- 동일 원본 카드 여러 장 등록 지원
- 누락 카드와 빈 덱 경고 처리
- 런타임 덱 생성 결과 Console 출력

### 4. 테스트 카드 데이터 생성

- `SO_Card_TestStrike` 생성
- `SO_Card_TestGuard` 생성
- `SO_Card_TestFocus` 생성
- 공격 카드 2장, 방어 카드 1장, 자원 카드 1장으로 테스트 덱 구성

### 5. 전투 씬 연결 및 검증

- `20_Battle` 씬에 `DeckRuntimeSystem` 생성
- `BattleDeckRuntime` 컴포넌트 연결
- Original Cards 목록에 테스트 카드 4장 연결
- 실행 시 Runtime Cards 4장 생성 확인
- 동일 카드 2장의 Instance ID 차이 확인
- 런타임 비용 변경 후 원본 카드 비용 유지 확인
- Play 종료 후 런타임 데이터 초기화 확인

## 추가 및 수정된 주요 파일

- `Assets/_ProjectC/Scripts/Data/CardRank.cs`
- `Assets/_ProjectC/Scripts/Data/CardType.cs`
- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDeckRuntime.cs`
- `Assets/_ProjectC/Data/Cards/SO_Card_TestStrike.asset`
- `Assets/_ProjectC/Data/Cards/SO_Card_TestGuard.asset`
- `Assets/_ProjectC/Data/Cards/SO_Card_TestFocus.asset`
- `Assets/_ProjectC/Scenes/20_Battle.unity`

## 테스트 결과

- [x] 테스트 카드 데이터 3종 생성
- [x] 원본 카드 덱 4장 구성
- [x] 런타임 카드 4장 생성
- [x] 동일 원본 카드의 독립 인스턴스 생성
- [x] 카드별 고유 Instance ID 생성
- [x] 런타임 비용과 원본 비용 분리
- [x] 빈 덱 예외 처리
- [x] 누락 카드 예외 처리
- [x] Play 종료 후 런타임 데이터 초기화
- [x] Console 빨간색 오류 0개

## 완료 결과

`20_Battle` 씬에서 원본 `CardData`를 기반으로 전투 전용 `CardInstance`를 생성하는 덱 런타임 구조를 완성했다. 이후 카드 더미, 손패, 버린 카드 더미, 셔플 및 드로우 기능을 연결할 수 있는 기반을 마련했다.