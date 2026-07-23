9일차 개발 일지
작업 목표

전투 중 플레이어가 보유한 카드를 화면에서 확인하고, 카드를 선택하여 AP를 소비해 사용할 수 있는 기본 손패 UI를 구현한다.

구현 내용
1. 전투 손패 UI 구현
현재 손패의 카드를 UI 프리팹으로 자동 생성
카드 이름, 비용, 설명, 종류, 등급 표시
카드 이미지가 없는 경우 이미지 영역 숨김
가로 스크롤을 이용한 다수 카드 확인
카드 선택 시 크기 확대와 배경색 강조
2. AP 상태 UI 구현
현재 AP와 최대 AP 표시
카드 사용 시 AP 자동 차감
AP 변경 시 UI 자동 갱신
AP가 부족한 카드의 비용을 붉은색으로 표시
AP가 부족하면 카드 사용 버튼 비활성화
AP가 없어도 0비용 카드 사용 가능
3. 카드 선택 및 사용 구현
손패에서 카드 한 장 선택
선택한 카드 이름 표시
선택 카드 사용 버튼 구현
카드 사용 가능 여부 검사
사용 성공 시 AP 차감
사용한 카드를 손패에서 제거
사용한 카드를 버린 카드 더미로 이동
사용 후 선택 상태 초기화
4. 카드 더미 상태 UI 구현

다음 카드 더미의 수량을 전투 화면에 표시하도록 구현했다.

뽑을 카드 더미
현재 손패
버린 카드 더미
제외 카드 더미

카드 드로우, 카드 사용, 손패 전체 버리기, 카드 더미 초기화가 실행될 때 UI가 자동으로 갱신되도록 이벤트를 연결했다.

5. 카드 UI 프리팹 구현

PF_BattleCardView 프리팹을 제작하고 다음 UI 요소를 구성했다.

카드 이름
카드 비용
카드 이미지
카드 설명
카드 종류
카드 등급
선택 버튼
선택 강조 효과
6. 손패 레이아웃 수정

카드들이 같은 위치에 겹쳐 표시되는 문제를 수정했다.

Card Container를 HandScrollView/Viewport/Content에 연결
Horizontal Layout Group 설정
Content Size Fitter 설정
카드 프리팹의 Layout Element 크기 설정
카드 내부 텍스트와 이미지 위치 조정
손패 카드가 왼쪽부터 가로로 정렬되도록 수정
7. 컴파일 오류 수정

System.Random과 UnityEngine.Random 사이의 모호한 참조로 발생한 CS0104 오류를 수정했다.

int randomIndex = UnityEngine.Random.Range(0, currentIndex + 1); // Unity 무작위 위치 선택
8. 테스트 결과
전투 시작 시 손패 카드 자동 생성 확인
카드 선택 강조 표시 확인
카드 사용 후 AP 차감 확인
카드 사용 후 손패 감소 확인
버린 카드 더미 증가 확인
AP 부족 시 사용 버튼 비활성화 확인
0비용 카드 사용 확인
손패 전체 버리기 후 UI 갱신 확인
카드 드로우 후 UI 갱신 확인
카드 더미 초기화 후 UI 복구 확인
가로 스크롤 동작 확인
카드 겹침 현상 수정 확인
Console 컴파일 오류 해결
주요 작업 파일
Assets/_ProjectC/Scripts/Battle/BattleDeckRuntime.cs
Assets/_ProjectC/Scripts/UI/Battle/BattleCardView.cs
Assets/_ProjectC/Scripts/UI/Battle/BattleHandUI.cs
Assets/_ProjectC/Prefabs/UI/PF_BattleCardView.prefab
Assets/_ProjectC/Scenes/20_Battle.unity
완료 결과

전투 손패와 AP 정보를 화면에서 확인할 수 있으며, 플레이어가 카드를 선택하고 AP를 소비하여 사용하는 기본 상호작용을 완성했다.

카드를 사용하거나 드로우하는 등 카드 더미에 변화가 발생하면 손패와 상태 UI도 자동으로 갱신된다.