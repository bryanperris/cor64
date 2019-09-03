import React from 'react';
import { connect } from 'react-redux';
import { RECV_UPDATE_DISASSEMBLY } from "../actions"

const listStyle = {
  listStyleType: "none",
  paddingLeft: "0px"
}

export class DisassemblyView extends React.Component {
  render () {
    return <div className="container bg-secondary rounded">
      <h6 className="text-center">Disassembly</h6>
      <ul style={listStyle}>{this.props.disasm}</ul>
    </div>
  }
}

const mapStateToProps = (state /*, ownProps */) => {
  return {
    disasm: state.disassembly.lines.map((line) => <li key={line.toString()}>{line}</li>)
  }
}

const mapDispatchToProps = { RECV_UPDATE_DISASSEMBLY }

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(DisassemblyView);
