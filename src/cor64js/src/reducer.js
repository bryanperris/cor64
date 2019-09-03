import { RECV_UPDATE_DISASSEMBLY } from './actions';

const initialState = {
  test: "test",
  disassembly: {
    lines: [
      "test 1",
      "test 2",
      "test 3"
    ]
  }
};

export default function reducer (state, action) {
  if (state === undefined) {
    return initialState;
  }

  switch (action.type) {
    default: return state;

    case RECV_UPDATE_DISASSEMBLY: {
      const { type, ...newState } = action;
      return Object.assign({}, state, newState);
    }
  }
}
