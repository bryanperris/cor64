import React from 'react';
import ReactDOM from 'react-dom';
import { createStore } from 'redux';
import { Provider } from 'react-redux';
import reducer from './reducer';
import DisassemblyView from './components/disassemblyView';

const store = createStore(reducer);

class App extends React.Component {
  constructor (props) {
    super(props);
    this.m_WebSocket = null;

    this.state = {
      message: ""
    };
  }

  bindSocketEvents () {
    if (this.m_WebSocket != null) {
      this.m_WebSocket.onerror = (e) => {
        this.setState({ message: "web socket reached an error" });
      }

      this.m_WebSocket.onopen = () => {
        this.m_WebSocket.send(JSON.stringify({ 'command': 'sync' }));
      };

      this.m_WebSocket.onmessage = (msg) => {
        const data = JSON.parse(msg.data);
        this.setState({ message: JSON.stringify(data) });
        store.dispatch(data);
      };
    }
  }

  componentWillMount () {
    this.m_WebSocket = new WebSocket(this.props.url);
    this.bindSocketEvents();
  }

  componentWillUnmount () {
    if (this.m_WebSocket != null) {
      this.m_WebSocket.close();
    }
  }

  render () {
    return <div>
      <div >DebugMsg: {this.state.message}</div>
      <br />
      <div className="container-fluid">
        <div className="row">
          <div className="col-sm">
            <DisassemblyView />
          </div>
          <div className="col-sm">
            Registers
          </div>
        </div>
      </div>
    </div>
  }
}

ReactDOM.render(
  <Provider store={store}>
    <App url="ws://localhost:6464" />
  </Provider>
  , document.getElementById('app'));
